using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OJTMISApi;
using OJTMISApi.Controllers;
using OJTMISApi.Data;
using OJTMISApi.Models;
using System.Net.Http.Headers;
using OJTMISApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Every stored date/time is office-local (Philippine) time, independent of
// the server's own time zone. See AppClock.
AppClock.Configure(builder.Configuration["App:TimeZone"]);

// `dotnet OJTMISApi.dll --migrate` applies pending migrations, seeds roles and
// the admin account, then exits. Use it in deployments where
// Database:MigrateOnStartup is off.
var migrateOnly = args.Contains("--migrate", StringComparer.OrdinalIgnoreCase);

// ---------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<NotificationInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(serviceProvider.GetRequiredService<NotificationInterceptor>()));

// ---------------------------------------------------------------------
// Identity + JWT
// ---------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

// Secrets are never stored in appsettings.json. Locally they come from
// `dotnet user-secrets`; in production from environment variables
// (Jwt__SigningKey, Mailtrap__ApiToken, Seed__AdminPassword).
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine(" ERROR: Jwt:SigningKey is missing or shorter than 32 bytes.");
    Console.Error.WriteLine();
    Console.Error.WriteLine(" Set it in the local secret store (never in appsettings.json):");
    Console.Error.WriteLine("     dotnet user-secrets set \"Jwt:SigningKey\" \"<64+ random chars>\" --project OJTMISApi");
    Console.Error.WriteLine(" or, on a server, the environment variable Jwt__SigningKey.");
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine();
    Environment.Exit(1);
}

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // Re-check the account on every request: a password change/reset,
    // lockout, or lost approval ends existing sessions immediately.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = JwtTokenService.ValidateAsync
    };
});

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddAuthRateLimits();

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// Email - Mailtrap Email API. A sandbox token posted to the transactional
// host (or the reverse) is what Mailtrap's edge answers with HTTP 502.
// InboxId > 0 selects the sandbox host; otherwise the transactional host
// https://send.api.mailtrap.io is used. The token is read from
// configuration or MAILTRAP_API_TOKEN, never from source code.
// ---------------------------------------------------------------------
builder.Services.Configure<MailtrapOptions>(
    builder.Configuration.GetSection(MailtrapOptions.SectionName));

builder.Services.AddHttpClient(MailtrapEmailService.HttpClientName, client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("OJTMIS/1.0 (Mailtrap Email API)");
});

builder.Services.AddScoped<IEmailService, MailtrapEmailService>();
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailQueueWorker>();
builder.Services.AddSingleton<AttendanceQrService>();
builder.Services.AddScoped<AttendanceLogService>();
builder.Services.AddScoped<InternCompletionService>();
builder.Services.AddScoped<AttendanceAdjustmentReviewService>();
builder.Services.AddScoped<DepartmentScopeService>();
builder.Services.AddScoped<AccountProvisioningService>();

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------
builder.Services.AddControllers();

// Errors: every failure reply is JSON with a `message` the Vue client shows.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var problem = context.ProblemDetails;
        if (problem is Microsoft.AspNetCore.Mvc.ValidationProblemDetails validation && validation.Errors.Count > 0)
        {
            // Field errors such as "Full Name is required." are what the user needs to see.
            problem.Extensions["message"] = string.Join(" ", validation.Errors.Values.SelectMany(v => v).Distinct());
        }
        else if (!problem.Extensions.ContainsKey("message"))
        {
            problem.Extensions["message"] = problem.Status switch
            {
                StatusCodes.Status401Unauthorized => "Your session has ended. Please sign in again.",
                StatusCodes.Status403Forbidden => "You do not have permission to do that.",
                StatusCodes.Status404NotFound => "The requested item or page was not found.",
                StatusCodes.Status405MethodNotAllowed => "That action is not supported here.",
                StatusCodes.Status415UnsupportedMediaType => "The request format is not supported.",
                _ => problem.Title ?? "The request could not be completed."
            };
        }
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// HTTPS: browsers are told to use HTTPS only (HSTS) outside Development.
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = false;
});

// Behind a reverse proxy (IIS / nginx / a cloud load balancer) the real
// client IP and scheme arrive in X-Forwarded-* headers. Only trust them
// when ReverseProxy:Enabled is set, otherwise anyone could spoof their IP
// past the rate limiter.
var behindProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindProxy)
{
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        foreach (var proxy in builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
        {
            if (System.Net.IPAddress.TryParse(proxy, out var ip)) options.KnownProxies.Add(ip);
        }
    });
}

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------------------------------------------------
// CORS - allows the Vue 3 dev server (http://localhost:5173) to call this API.
// ---------------------------------------------------------------------
const string CorsPolicy = "AllowVueDevServer";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition");
    });
});

var app = builder.Build();

// ---------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------
if (behindProxy)
{
    app.UseForwardedHeaders();
}

// Unhandled exceptions and empty error replies (401/403/404) become JSON
// ProblemDetails with a readable `message`.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OJTMIS API v1");
        options.DocumentTitle = "OJTMIS API";
    });
}
else
{
    // In Development the Vite proxy talks plain HTTP to the API, so these
    // only apply to real deployments.
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Basic hardening headers for every API reply.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    await next();
});

// Must sit between UseRouting() and the endpoint middleware.
app.UseCors(CorsPolicy);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ActivityLogMiddleware>();

app.MapControllers();

// ---------------------------------------------------------------------
// Database schema. Development applies pending migrations automatically so
// a fresh database just works. Elsewhere (Database:MigrateOnStartup=false by
// default) the schema is upgraded once, deliberately, with `--migrate`, so
// several app instances never race to migrate the same database, and the app
// refuses to start against an out-of-date schema.
// ---------------------------------------------------------------------
var migrateOnStartup = app.Configuration.GetValue<bool?>("Database:MigrateOnStartup")
                       ?? app.Environment.IsDevelopment();

if (migrateOnly || migrateOnStartup)
{
    await ApplyMigrationsAsync(app);
}
else
{
    await EnsureSchemaIsCurrentAsync(app);
}

// ---------------------------------------------------------------------
// Seed: roles + default HR Admin account (idempotent).
// ---------------------------------------------------------------------
await SeedAsync(app);

if (migrateOnly)
{
    Console.WriteLine("Database migrated and seeded. Exiting (--migrate).");
    return;
}

// Pre-flight port check. Binding failures inside Kestrel surface as a deeply
// wrapped IOException with a 40-line stack trace, so detect the busy port up
// front and print something actionable instead.
var portInUse = FindBusyPort(app.Configuration["urls"]);
if (portInUse is not null)
{
    PrintPortInUse(portInUse.Value.port, portInUse.Value.url);
    return;
}

app.Run();

// Walks the configured listen addresses (ASPNETCORE_URLS, set from
// launchSettings.json) and returns the first HTTP port already in use.
static (int port, string url)? FindBusyPort(string? configuredUrls)
{
    var urls = configuredUrls;

    if (string.IsNullOrWhiteSpace(urls))
    {
        urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    }

    if (string.IsNullOrWhiteSpace(urls))
    {
        return null;
    }

    foreach (var raw in urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        // The Vite proxy only talks HTTP, so only the http entries matter.
        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) continue;
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)) continue;
        if (uri.Port <= 0) continue;

        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, uri.Port);
            listener.Start();
            listener.Stop();
        }
        catch (System.Net.Sockets.SocketException)
        {
            // Port is taken by another process.
            return (uri.Port, raw);
        }
    }

    return null;
}

static void PrintPortInUse(int port, string url)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine($" ERROR: Port {port} is already in use.");
    Console.Error.WriteLine($" Another process is already listening on {url}.");
    Console.Error.WriteLine();
    Console.Error.WriteLine(" Most likely the backend was started twice, or an earlier");
    Console.Error.WriteLine(" run is still alive in another terminal.");
    Console.Error.WriteLine();
    Console.Error.WriteLine(" Fix - stop the other instance, then run again:");
    Console.Error.WriteLine("     npm run stop");
    Console.Error.WriteLine();
    Console.Error.WriteLine(" Or just use the self-cleaning launcher, which frees the");
    Console.Error.WriteLine(" port for you and starts both apps:");
    Console.Error.WriteLine("     npm run dev");
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine();
    Environment.Exit(1);
}

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Nothing to do when the database does not exist yet and the server
    // will not create it (e.g. limited rights) - surface a clear message.
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("===========================================================");
        Console.Error.WriteLine(" ERROR: Could not apply database migrations.");
        Console.Error.WriteLine($" {ex.Message}");
        Console.Error.WriteLine();
        Console.Error.WriteLine(" Check the 'DefaultConnection' connection string and");
        Console.Error.WriteLine(" that the target database exists and is reachable.");
        Console.Error.WriteLine(" On your own machine, set it in the local secret store:");
        Console.Error.WriteLine("     dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Server=...;Database=...;Integrated Security=True;TrustServerCertificate=True\" --project OJTMISApi");
        Console.Error.WriteLine("===========================================================");
        Console.Error.WriteLine();
        Environment.Exit(1);
    }
}

static async Task EnsureSchemaIsCurrentAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
    if (pending.Count == 0) return;

    Console.Error.WriteLine();
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine($" ERROR: The database is missing {pending.Count} migration(s):");
    foreach (var name in pending) Console.Error.WriteLine($"     {name}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(" Upgrade it once, then start the app normally:");
    Console.Error.WriteLine("     dotnet OJTMISApi.dll --migrate");
    Console.Error.WriteLine(" (or set Database__MigrateOnStartup=true for a single-instance setup)");
    Console.Error.WriteLine("===========================================================");
    Console.Error.WriteLine();
    Environment.Exit(1);
}

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in UserRoles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var config = app.Configuration;
    var email = config["Seed:AdminEmail"] ?? "hradmin@ojtmis.local";
    var password = config["Seed:AdminPassword"];

    if (await userManager.FindByEmailAsync(email) is null)
    {
        // No hardcoded fallback: a public default password would let anyone
        // sign in as HR on a fresh install.
        if (string.IsNullOrWhiteSpace(password))
        {
            app.Logger.LogWarning(
                "Seed:AdminPassword is not set, so the default HR Admin account ({Email}) was not created. " +
                "Set it with: dotnet user-secrets set \"Seed:AdminPassword\" \"<password>\" --project OJTMISApi",
                email);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsApproved = true,
            FirstName = "HR",
            LastName = "Administrator",
            Office = "ISDMD"
        };

        var result = await userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, UserRoles.HRAdmin);
        }
    }
}

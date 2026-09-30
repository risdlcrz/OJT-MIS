using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OJTMISApi.Controllers;
using OJTMISApi.Data;
using OJTMISApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---------------------------------------------------------------------
// Identity + JWT
// ---------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

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
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------
builder.Services.AddControllers();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OJTMIS API v1");
        options.DocumentTitle = "OJTMIS API";
    });
}

// Must sit between UseRouting() and the endpoint middleware.
app.UseCors(CorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ---------------------------------------------------------------------
// Seed: roles + default HR Admin account.
// ---------------------------------------------------------------------
await SeedAsync(app);

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
    var password = config["Seed:AdminPassword"] ?? "Admin@12345";

    if (await userManager.FindByEmailAsync(email) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
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

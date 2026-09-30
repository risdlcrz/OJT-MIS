using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

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

app.UseAuthorization();

app.MapControllers();

app.Run();

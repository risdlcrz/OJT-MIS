using System.Security.Claims;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Dinaragdagan ang ActivityLogs ng isang entry para sa bawat
    /// authenticated na write request (POST/PUT/PATCH/DELETE). Naka-grupo
    /// ang mga entry ayon sa role ng user: HR, Supervisor, o Intern.
    /// </summary>
    public class ActivityLogMiddleware
    {
        private static readonly HashSet<string> IgnoredPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "/api/logs",
            "/api/notifications",
            // Written by HRController with the intern name and document type.
            "/api/hr/documents",
            // Login is logged explicitly by AuthController; avoid duplicates.
            "/api/auth"
        };

        private readonly RequestDelegate _next;

        public ActivityLogMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            await _next(context);

            if (!ShouldLog(context))
            {
                return;
            }

            try
            {
                var user = context.User;
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var category = roles.Contains(UserRoles.HRAdmin) ? "HR"
                    : roles.Contains(UserRoles.Supervisor) ? "Supervisor"
                    : "Intern";

                var path = context.Request.Path.Value ?? string.Empty;
                var method = context.Request.Method.ToUpperInvariant();

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                    UserName = user.FindFirstValue(ClaimTypes.Name) ?? "Unknown user",
                    Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                    Category = category,
                    Method = method,
                    Path = path,
                    Action = Describe(method, path),
                    Details = context.Response.StatusCode < 400
                        ? "Action completed successfully."
                        : "Action was not completed.",
                    IsVerified = context.Response.StatusCode < 400,
                    CreatedAt = DateTime.Now
                });

                await db.SaveChangesAsync();
            }
            catch
            {
                // Huwag hayaang maapektuhan ng audit logging ang request.
            }
        }

        private static bool ShouldLog(HttpContext context)
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false)) return false;

            var method = context.Request.Method;
            if (method is not ("POST" or "PUT" or "PATCH" or "DELETE")) return false;

            var path = context.Request.Path.Value ?? string.Empty;
            return !IgnoredPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string Describe(string method, string path)
        {
            var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var resource = parts.Length > 1 ? parts[1].ToLowerInvariant() : "record";
            var last = parts.Length > 0 ? parts[^1].ToLowerInvariant() : string.Empty;

            if (parts.Length >= 4 && parts[1].Equals("HR", StringComparison.OrdinalIgnoreCase))
            {
                var area = parts[2].ToLowerInvariant();
                var verb = parts[3].ToLowerInvariant();
                if (area == "attendancerequests" && verb == "approve") return "Approved a time adjustment request";
                if (area == "attendancerequests" && verb == "reject") return "Rejected a time adjustment request";
            }

            if (resource == "applicants" && last == "hire") return "Approved and hired an applicant";
            if (resource == "account" && last == "change-password") return "Changed account password";
            if (resource == "account" && last == "profile") return "Updated account profile";
            if (resource == "intern" && last == "scan") return "Scanned intern ID for DTR";
            if (resource == "attendance" && last == "logtime") return "Logged intern DTR time";
            if (resource == "attendance" && last == "requestadjustment") return "Submitted a time adjustment request";
            if (resource == "intern" && last == "documents") return "Requested an intern document";
            if (resource == "admin" && parts.Length > 2) resource = parts[2].ToLowerInvariant();

            var label = resource switch
            {
                "schools" => "school",
                "signatories" => "signatory",
                "applicants" => "applicant",
                "interns" => "intern",
                "intern" => "intern portal",
                "internrequests" => "intern request",
                "programs" => "program",
                "users" => "user profile",
                _ => resource
            };

            return method switch
            {
                "POST" => $"Created a {label}",
                "PUT" or "PATCH" => $"Updated a {label}",
                "DELETE" => $"Deleted a {label}",
                _ => $"{method} {path}"
            };
        }
    }
}

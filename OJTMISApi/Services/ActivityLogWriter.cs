using System.Security.Claims;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Writes one activity-log row. Callers save the DbContext themselves.
    /// </summary>
    public static class ActivityLogWriter
    {
        public static void Add(
            ApplicationDbContext db,
            ClaimsPrincipal principal,
            string method,
            string path,
            string action,
            string? details = null)
        {
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown user",
                Email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Category = CategoryFor(roles),
                Method = method,
                Path = path,
                Action = action,
                Details = string.IsNullOrWhiteSpace(details) ? "Action completed successfully." : details,
                IsVerified = true,
                CreatedAt = DateTime.Now
            });
        }

        public static string CategoryFor(IEnumerable<string> roles)
        {
            var list = roles as ICollection<string> ?? roles.ToList();
            if (list.Contains(UserRoles.HRAdmin)) return "HR";
            if (list.Contains(UserRoles.Supervisor)) return "Supervisor";
            return "Intern";
        }
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Awtomatikong gumagawa ng notification kapag may Create, Update, o
    /// Delete sa Requests, Signatories, Schools, Applicants, at Interns.
    /// </summary>
    public sealed class NotificationInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NotificationInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            AddNotifications(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AddNotifications(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void AddNotifications(DbContext? context)
        {
            if (context is null) return;

            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return;

            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => Describe(e) is not null)
                .ToList();

            if (entries.Count == 0) return;

            var userName = httpContext?.User.FindFirstValue(ClaimTypes.Name);
            var by = string.IsNullOrWhiteSpace(userName) ? string.Empty : $" by {userName}";
            var now = DateTime.Now;

            foreach (var entry in entries)
            {
                var info = Describe(entry);
                if (info is null) continue;

                context.Add(new Notification
                {
                    UserId = userId,
                    Title = $"{info.Value.Subject} {info.Value.Action}",
                    Message = $"{info.Value.Label} was {info.Value.Action}{by}.",
                    Module = info.Value.Module,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            // Isama agad sa parehong SaveChanges ang mga bagong notification.
            context.ChangeTracker.DetectChanges();
        }

        private static (string Module, string Subject, string Label, string Action)? Describe(EntityEntry entry)
        {
            var action = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Modified => "updated",
                EntityState.Deleted => "deleted",
                _ => null
            };

            if (action is null) return null;

            return entry.Entity switch
            {
                InternRequest request => (
                    "Applicant Requests",
                    "Applicant Request",
                    Label($"Request #{request.Id}", request.OfficeName),
                    action),

                Signatory signatory => (
                    "Signatories",
                    "Signatory",
                    Label($"Signatory #{signatory.Id}", signatory.Name),
                    action),

                School school => (
                    "Schools",
                    "School",
                    Label($"School #{school.Id}", school.Name),
                    action),

                Applicant applicant => (
                    "Applicants",
                    "Applicant",
                    Label(applicant.ApplicantNo, FullName(
                        applicant.FirstName, applicant.MiddleName, applicant.LastName, applicant.Suffix)),
                    action),

                Intern intern => (
                    "Interns",
                    "Intern",
                    Label(intern.ApplicantNo, intern.FullName),
                    action),

                _ => null
            };
        }

        private static string Label(string fallback, string? preferred)
        {
            var value = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred.Trim();
            return $"\"{value}\"";
        }

        private static string FullName(string? first, string? middle, string? last, string? suffix)
        {
            var name = string.Join(" ", new[] { first, middle, last, suffix }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

            return string.IsNullOrWhiteSpace(name) ? "Applicant" : name;
        }
    }
}

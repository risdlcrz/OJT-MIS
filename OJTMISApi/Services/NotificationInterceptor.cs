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

            var actingUserIsIntern = ActingUserIsIntern(httpContext);
            var entries = context.ChangeTracker.Entries()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Where(e => Describe(e, actingUserIsIntern) is not null)
                .ToList();

            if (entries.Count == 0) return;

            var userName = httpContext?.User.FindFirstValue(ClaimTypes.Name);
            var by = string.IsNullOrWhiteSpace(userName) ? string.Empty : $" by {userName}";
            var now = DateTime.Now;

            foreach (var entry in entries)
            {
                var info = Describe(entry, actingUserIsIntern);
                if (info is null) continue;

                context.Add(new Notification
                {
                    UserId = userId,
                    Title = $"{info.Subject} {info.Action}",
                    Message = $"{info.Label} was {info.Action}{by}.",
                    Module = info.Module,
                    Link = info.Link,
                    InternId = info.InternId,
                    Type = info.Type,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            // Isama agad sa parehong SaveChanges ang mga bagong notification.
            context.ChangeTracker.DetectChanges();
        }

        /// <summary>
        /// Kapag ang nag-CRD ang intern mismo (hal. document request sa portal),
        /// ang tamang destinasyon ay ang intern page, hindi ang HR page.
        /// </summary>
        private static bool ActingUserIsIntern(HttpContext? httpContext) =>
            httpContext?.User.IsInRole(UserRoles.Intern) ?? false;

        /// <summary>
        /// Notification description: what to show plus where the item should
        /// navigate when clicked (page link, intern id, machine type).
        /// </summary>
        private sealed record NotificationInfo(
            string Module,
            string Subject,
            string Label,
            string Action,
            string? Type = null,
            string? Link = null,
            int? InternId = null);

        private static NotificationInfo? Describe(EntityEntry entry, bool actingUserIsIntern)
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
                InternRequest request => new NotificationInfo(
                    "Applicant Requests",
                    "Applicant Request",
                    Label($"Request #{request.Id}", request.OfficeName),
                    action,
                    Type: "InternRequest",
                    Link: "/requests.html"),

                Signatory signatory => new NotificationInfo(
                    "Signatories",
                    "Signatory",
                    Label($"Signatory #{signatory.Id}", signatory.Name),
                    action,
                    Type: "Signatory",
                    Link: "/signatories.html"),

                School school => new NotificationInfo(
                    "Schools",
                    "School",
                    Label($"School #{school.Id}", school.Name),
                    action,
                    Type: "School",
                    Link: "/schools.html"),

                Applicant applicant => new NotificationInfo(
                    "Applicants",
                    "Applicant",
                    Label(applicant.ApplicantNo, FullName(
                        applicant.FirstName, applicant.MiddleName, applicant.LastName, applicant.Suffix)),
                    action,
                    Type: "Applicant",
                    Link: "/applicants.html"),

                Intern intern => new NotificationInfo(
                    "Interns",
                    "Intern",
                    Label(intern.ApplicantNo, intern.FullName),
                    action,
                    Type: "Intern",
                    Link: "/internlist.html",
                    InternId: intern.Id),

                DocumentRequest document => new NotificationInfo(
                    "Documents",
                    "Document Request",
                    Label(document.DocumentType, document.InternName),
                    action,
                    Type: "DocumentRequested",
                    // Interns track their own requests; HR prepares the document
                    // on the matching generation page.
                    Link: actingUserIsIntern
                        ? "/intern-documents.html"
                        : string.Equals(document.DocumentType, "ID", StringComparison.OrdinalIgnoreCase)
                            ? "/idgenerate.html"
                            : "/coc-generate.html",
                    InternId: document.InternId),

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

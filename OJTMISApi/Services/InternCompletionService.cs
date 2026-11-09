using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Recomputes an intern's rendered hours and, once the required total is
    /// reached, marks the intern as Completed and sends a one-time notification
    /// to every HR admin so the Certificate of Completion can be generated.
    /// </summary>
    public class InternCompletionService
    {
        public const string CompletedStatus = "Completed";
        public const string CompletionNotificationType = "InternCompleted";
        public const string CertificateModule = "Certificate of Completion";

        /// <summary>App route stored on the HR notification. The bell maps this to the COC page.</summary>
        public const string CertificateLink = "/requests/certificate-completion";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InternCompletionService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>Total hours from attendance logs that already have a time out.</summary>
        public async Task<double> RenderedHoursAsync(int internId, CancellationToken cancellationToken = default)
        {
            var records = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == internId && x.TimeOut != null)
                .Select(x => new { x.TimeIn, x.TimeOut })
                .ToListAsync(cancellationToken);

            return records.Sum(x => AttendanceShift.Hours(x.TimeIn, x.TimeOut));
        }

        /// <summary>
        /// Saves the recalculated hours. When they reach the required total and
        /// the intern is not already Completed, marks them Completed and notifies HR once.
        /// </summary>
        /// <returns>True when this call marked the intern as completed.</returns>
        public async Task<bool> CheckAsync(
            int internId,
            double? renderedHours = null,
            CancellationToken cancellationToken = default)
        {
            var intern = await _context.Interns
                .FirstOrDefaultAsync(i => i.Id == internId, cancellationToken);

            if (intern is null) return false;

            var rendered = Math.Round(renderedHours ?? await RenderedHoursAsync(internId, cancellationToken), 2);
            var now = DateTime.Now;
            intern.HoursRendered = rendered;
            intern.UpdatedAt = now;

            var newlyCompleted = intern.RequiredHours > 0
                && rendered + 0.009 >= intern.RequiredHours
                && !string.Equals(intern.Status, CompletedStatus, StringComparison.OrdinalIgnoreCase);

            if (newlyCompleted)
            {
                intern.Status = CompletedStatus;
                intern.CompletionNotifiedAt = now;

                var hrUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HRAdmin);
                foreach (var hr in hrUsers)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = hr.Id,
                        Title = "Completion Eligible",
                        Message = $"{intern.FullName} completed required hours.",
                        Module = CertificateModule,
                        Link = CertificateLink,
                        InternId = intern.Id,
                        Type = CompletionNotificationType,
                        IsRead = false,
                        CreatedAt = now
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return newlyCompleted;
        }
    }
}

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

        /// <summary>Frontend page that hosts the certificate generator.</summary>
        public const string CertificatePage = "/coc-generate.html";

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
        /// Marks the intern Completed and notifies HR when the rendered hours
        /// reach the required hours. Safe to call after every punch/approval:
        /// the <see cref="Intern.CompletionNotifiedAt"/> guard keeps it one-time.
        /// </summary>
        /// <returns>True when this call marked the intern as completed.</returns>
        public async Task<bool> CheckAsync(
            int internId,
            double? renderedHours = null,
            CancellationToken cancellationToken = default)
        {
            var intern = await _context.Interns
                .FirstOrDefaultAsync(i => i.Id == internId, cancellationToken);

            if (intern is null || intern.RequiredHours <= 0) return false;
            if (intern.CompletionNotifiedAt is not null) return false;

            var rendered = renderedHours ?? await RenderedHoursAsync(internId, cancellationToken);
            if (rendered + 0.009 < intern.RequiredHours) return false;

            var now = DateTime.Now;
            intern.Status = CompletedStatus;
            intern.CompletionNotifiedAt = now;
            intern.UpdatedAt = now;

            var hrUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HRAdmin);
            var link = $"{CertificatePage}?internId={intern.Id}";

            foreach (var hr in hrUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = hr.Id,
                    Title = "Intern completed required hours",
                    Message = $"Intern {intern.FullName} has completed their required hours and is ready for COC generation.",
                    Module = CertificateModule,
                    Link = link,
                    InternId = intern.Id,
                    Type = CompletionNotificationType,
                    IsRead = false,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}

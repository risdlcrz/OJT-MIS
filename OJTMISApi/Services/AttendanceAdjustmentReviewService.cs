using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Approves or rejects an attendance time adjustment request. Shared by
    /// the HR review screen and the department supervisor portal so both apply
    /// the punch change, notify the intern, log the action, and re-check
    /// completion the same way.
    /// </summary>
    public sealed class AttendanceAdjustmentReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InternCompletionService _completion;

        public AttendanceAdjustmentReviewService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            InternCompletionService completion)
        {
            _context = context;
            _userManager = userManager;
            _completion = completion;
        }

        /// <summary>HTTP status plus response body for the calling controller.</summary>
        public sealed record Outcome(int StatusCode, object Body);

        /// <param name="id">Adjustment request id.</param>
        /// <param name="approve">True to approve, false to reject.</param>
        /// <param name="reviewer">The signed-in HR admin or supervisor.</param>
        /// <param name="path">Request path recorded in the activity log.</param>
        /// <param name="inScope">
        /// Optional access check on the request's intern. When it returns
        /// false the request is reported as not found, so a supervisor cannot
        /// learn about requests outside their department.
        /// </param>
        /// <param name="reviewedBySuffix">Appended to the intern's notification, e.g. " by your department supervisor".</param>
        public async Task<Outcome> ReviewAsync(
            int id,
            bool approve,
            ClaimsPrincipal reviewer,
            string path,
            Func<Intern?, bool>? inScope = null,
            string reviewedBySuffix = "")
        {
            var notFound = new Outcome(StatusCodes.Status404NotFound,
                new { message = $"Time adjustment request {id} was not found." });

            var request = await _context.AttendanceAdjustmentRequests
                .Include(r => r.Intern)
                .Include(r => r.AttendanceLog)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return notFound;

            var log = request.AttendanceLog;
            var intern = request.Intern
                ?? (log?.InternId is int logInternId
                    ? await _context.Interns.FirstOrDefaultAsync(i => i.Id == logInternId)
                    : null);

            if (inScope is not null && !inScope(intern)) return notFound;

            if (!string.Equals(request.Status, AttendanceAdjustmentStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return new Outcome(StatusCodes.Status409Conflict,
                    new { message = $"This request has already been {request.Status.ToLowerInvariant()}." });
            }

            var now = AppClock.Now;
            var label = AttendanceAdjustmentField.Label(request.TargetField);

            if (approve)
            {
                var error = ApplyToLog(request, log, out label);
                if (error is not null) return new Outcome(StatusCodes.Status400BadRequest, new { message = error });
                log!.UpdatedAt = now;
                request.Status = AttendanceAdjustmentStatus.Approved;
            }
            else
            {
                request.Status = AttendanceAdjustmentStatus.Rejected;
            }

            var reviewerId = reviewer.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewedBy = reviewerId;
            request.ApproverSignatoryId = await ResolveApproverSignatoryIdAsync(reviewerId);
            request.ReviewedAt = now;

            var when = log?.Date.ToString("MMM d, yyyy") ?? "the selected day";
            var verb = approve ? "approved" : "rejected";
            await NotifyInternAsync(
                intern,
                $"Time adjustment {verb}",
                $"Your {label} adjustment for {when} was {verb}{reviewedBySuffix}.",
                now);

            var internName = intern?.FullName ?? log?.InternName ?? "an intern";
            ActivityLogWriter.Add(
                _context,
                reviewer,
                "POST",
                path,
                approve ? "Approved a time adjustment request" : "Rejected a time adjustment request",
                $"{(approve ? "Approved" : "Rejected")} {internName}'s {label} adjustment for {when}.");

            await _context.SaveChangesAsync();

            double? dailyHours = null;
            var completed = false;
            if (approve && log is not null)
            {
                dailyHours = await CalculateDailyHoursAsync(request.InternId, log.Date);
                // If this approval brings the intern to the required hours,
                // mark them Completed and tell HR (once).
                completed = await _completion.CheckAsync(request.InternId);
            }

            return new Outcome(StatusCodes.Status200OK, new
            {
                message = approve ? "Time adjustment approved." : "Time adjustment rejected.",
                completed,
                request = new
                {
                    request.Id,
                    request.Status,
                    request.ReviewedAt,
                    request.TargetField,
                    request.ApproverSignatoryId
                },
                attendanceLog = log is null
                    ? null
                    : new { log.Id, log.Date, log.ShiftType, log.TimeIn, log.TimeOut, log.Status },
                dailyHours
            });
        }

        /// <summary>
        /// Writes the requested time onto the attendance log. Returns an error
        /// message when the change is invalid; the log is left untouched then.
        /// </summary>
        private static string? ApplyToLog(AttendanceAdjustmentRequest request, DailyTimeRecord? log, out string label)
        {
            label = AttendanceAdjustmentField.Label(request.TargetField);
            if (log is null) return "The linked attendance log no longer exists.";

            var field = AttendanceAdjustmentField.Normalize(request.TargetField);
            if (field is null) return "The request target field is not valid.";

            var logIsMorning = !string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase);
            if (AttendanceAdjustmentField.IsMorning(field) != logIsMorning)
            {
                return "The request's target field no longer matches the attendance log.";
            }

            label = AttendanceAdjustmentField.Label(field);
            var approvedTime = log.Date.Date.Add(request.RequestedTime.TimeOfDay);

            if (AttendanceAdjustmentField.IsTimeIn(field))
            {
                if (log.TimeOut is not null && approvedTime >= log.TimeOut.Value)
                {
                    return $"The requested {label} must be before the time out.";
                }

                log.TimeIn = approvedTime;
                log.Status = AttendanceShift.TimeInStatus(log.ShiftType, approvedTime);
            }
            else
            {
                if (approvedTime <= log.TimeIn)
                {
                    return $"The requested {label} must be after the time in.";
                }

                log.TimeOut = approvedTime;
            }

            return null;
        }

        /// <summary>The reviewer's DTR-scoped signatory record, if they have one.</summary>
        private async Task<int?> ResolveApproverSignatoryIdAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.Signatories
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                            (s.ApprovalScope == SignatoryApprovalScope.Dtr ||
                             s.ApprovalScope == SignatoryApprovalScope.Both))
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        private async Task NotifyInternAsync(Intern? intern, string title, string message, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(intern?.Email)) return;

            var user = await _userManager.FindByEmailAsync(intern.Email);
            if (user is null) return;

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                Module = "Attendance",
                Link = "/intern-attendance.html",
                InternId = intern.Id,
                Type = "AttendanceAdjustment",
                IsRead = false,
                CreatedAt = now
            });
        }

        private async Task<double> CalculateDailyHoursAsync(int internId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var records = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == internId && x.Date >= dayStart && x.Date < dayEnd)
                .ToListAsync();

            return Math.Round(records.Sum(x => AttendanceShift.Hours(x)), 2);
        }
    }
}

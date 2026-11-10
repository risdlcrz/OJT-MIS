using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Intern)]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceLogService _logs;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(
            AttendanceLogService logs,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logs = logs;
            _context = context;
            _userManager = userManager;
        }

        /// <summary>POST: api/Attendance/LogTime</summary>
        [HttpPost("LogTime")]
        public async Task<ActionResult<object>> LogTime([FromBody] ScanRequest request)
        {
            var result = await _logs.LogAsync(User, request.Payload);
            return StatusCode(result.Status, result.Body);
        }

        /// <summary>
        /// POST: api/Attendance/RequestAdjustment
        /// Submits a pending punch correction for one of the intern's logs.
        /// </summary>
        [HttpPost("RequestAdjustment")]
        public async Task<ActionResult<object>> RequestAdjustment([FromBody] AttendanceAdjustmentInput request)
        {
            var intern = await _logs.ResolveInternAsync(User);
            if (intern is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "This account is not linked to a hired intern record."
                });
            }

            var targetField = AttendanceAdjustmentField.Normalize(request.TargetField ?? request.TargetShift);
            var fieldLabel = targetField is null ? null : AttendanceAdjustmentField.Label(targetField);
            if (targetField is null || fieldLabel is null)
            {
                return BadRequest(new { message = "Choose AM Time In, AM Time Out, PM Time In, or PM Time Out." });
            }

            var reason = (request.Reason ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { message = "Reason is required." });
            }
            if (reason.Length > 500)
            {
                return BadRequest(new { message = "Reason cannot exceed 500 characters." });
            }

            var log = await _context.DailyTimeRecords
                .FirstOrDefaultAsync(x => x.Id == request.AttendanceLogId && x.InternId == intern.Id);
            if (log is null)
            {
                return NotFound(new { message = "Attendance log not found for this intern." });
            }

            var logIsMorning = !string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase);
            if (AttendanceAdjustmentField.IsMorning(targetField) != logIsMorning)
            {
                return BadRequest(new { message = $"The selected {fieldLabel} does not match this attendance log." });
            }

            var requestedRaw = request.RequestedTime ?? request.RequestedTimeOut;
            if (!TryParseRequestedTime(requestedRaw, log.Date, out var requestedTime))
            {
                return BadRequest(new { message = $"Enter a valid {fieldLabel} (HH:mm)." });
            }

            var hasPending = await _context.AttendanceAdjustmentRequests.AnyAsync(x =>
                x.AttendanceLogId == log.Id &&
                x.TargetField == targetField &&
                x.Status == AttendanceAdjustmentStatus.Pending);
            if (hasPending)
            {
                return Conflict(new { message = $"A pending {fieldLabel} request already exists for this attendance log." });
            }

            var now = DateTime.Now;
            var entity = new AttendanceAdjustmentRequest
            {
                AttendanceLogId = log.Id,
                InternId = intern.Id,
                RequestedTime = requestedTime,
                TargetField = targetField,
                Reason = reason,
                Status = AttendanceAdjustmentStatus.Pending,
                RequestedAt = now
            };

            _context.AttendanceAdjustmentRequests.Add(entity);

            var internUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(internUserId))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = internUserId,
                    Title = "Time adjustment submitted",
                    Message = $"Your {fieldLabel} adjustment for {log.Date:MMM d, yyyy} is pending approval.",
                    Module = "Attendance",
                    Link = "/intern-attendance.html",
                    InternId = intern.Id,
                    Type = "AttendanceAdjustment",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            var hrUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HRAdmin);
            foreach (var hr in hrUsers)
            {
                if (hr.Id == internUserId) continue;
                _context.Notifications.Add(new Notification
                {
                    UserId = hr.Id,
                    Title = "Time adjustment pending review",
                    Message = $"{intern.FullName} requested a {fieldLabel} adjustment for {log.Date:MMM d, yyyy}.",
                    Module = "Attendance",
                    Link = "/time-adjustments.html",
                    InternId = intern.Id,
                    Type = "AttendanceAdjustment",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Adjustment request submitted for approval.",
                request = new
                {
                    entity.Id,
                    entity.AttendanceLogId,
                    entity.InternId,
                    entity.RequestedTime,
                    entity.TargetField,
                    entity.Reason,
                    entity.Status,
                    entity.RequestedAt
                }
            });
        }

        private static bool TryParseRequestedTime(string? value, DateTime logDate, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var text = value.Trim();

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime))
            {
                result = logDate.Date.Add(dateTime.TimeOfDay);
                return true;
            }

            if (TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time))
            {
                result = logDate.Date.Add(time.ToTimeSpan());
                return true;
            }

            return false;
        }
    }

    public class AttendanceAdjustmentInput
    {
        public int AttendanceLogId { get; set; }

        /// <summary>"HH:mm" or a full ISO date/time. The log's date is used.</summary>
        public string? RequestedTime { get; set; }

        /// <summary>Accepted so older clients keep working.</summary>
        public string? RequestedTimeOut { get; set; }

        /// <summary>AM_In, AM_Out, PM_In, or PM_Out.</summary>
        public string? TargetField { get; set; }

        /// <summary>Accepted so older clients keep working.</summary>
        public string? TargetShift { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
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

        public AttendanceController(AttendanceLogService logs, ApplicationDbContext context)
        {
            _logs = logs;
            _context = context;
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
        /// Submits a pending time-out correction for one of the intern's logs.
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

            var targetShift = NormalizeTargetShift(request.TargetShift);
            if (targetShift is null)
            {
                return BadRequest(new { message = "Choose AM Time Out or PM Time Out." });
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

            var logShift = string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase)
                ? AttendanceAdjustmentShift.AfternoonOut
                : AttendanceAdjustmentShift.MorningOut;
            if (!string.Equals(logShift, targetShift, StringComparison.Ordinal))
            {
                return BadRequest(new { message = "The selected shift does not match this attendance log." });
            }

            if (!TryParseRequestedTimeOut(request.RequestedTimeOut, log.Date, out var requestedTimeOut))
            {
                return BadRequest(new { message = "Enter a valid time-out (HH:mm)." });
            }

            var hasPending = await _context.AttendanceAdjustmentRequests.AnyAsync(x =>
                x.AttendanceLogId == log.Id &&
                x.Status == AttendanceAdjustmentStatus.Pending);
            if (hasPending)
            {
                return Conflict(new { message = "A pending adjustment request already exists for this attendance log." });
            }

            var entity = new AttendanceAdjustmentRequest
            {
                AttendanceLogId = log.Id,
                InternId = intern.Id,
                RequestedTimeOut = requestedTimeOut,
                TargetShift = targetShift,
                Reason = reason,
                Status = AttendanceAdjustmentStatus.Pending,
                RequestedAt = DateTime.Now
            };

            _context.AttendanceAdjustmentRequests.Add(entity);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Adjustment request submitted for approval.",
                request = entity
            });
        }

        private static string? NormalizeTargetShift(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Equals(AttendanceAdjustmentShift.MorningOut, StringComparison.OrdinalIgnoreCase) ||
                text.Equals("AM Time Out", StringComparison.OrdinalIgnoreCase))
            {
                return AttendanceAdjustmentShift.MorningOut;
            }
            if (text.Equals(AttendanceAdjustmentShift.AfternoonOut, StringComparison.OrdinalIgnoreCase) ||
                text.Equals("PM Time Out", StringComparison.OrdinalIgnoreCase))
            {
                return AttendanceAdjustmentShift.AfternoonOut;
            }
            return null;
        }

        private static bool TryParseRequestedTimeOut(string? value, DateTime logDate, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var text = value.Trim();

            // Accept a full date/time or just a time (e.g. "14:30" or "2:30 PM").
            // Only the time-of-day is used; the date always follows the log.
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
        public string RequestedTimeOut { get; set; } = string.Empty;

        /// <summary>AM_Out / PM_Out (also accepts "AM Time Out" / "PM Time Out").</summary>
        public string TargetShift { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }
}

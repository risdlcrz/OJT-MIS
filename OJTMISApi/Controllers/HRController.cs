using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/HR")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class HRController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HRController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>GET: api/HR/AttendanceRequests/Pending</summary>
        [HttpGet("AttendanceRequests/Pending")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingAttendanceRequests()
        {
            var rows = await _context.AttendanceAdjustmentRequests
                .AsNoTracking()
                .Where(r => r.Status == AttendanceAdjustmentStatus.Pending)
                .OrderBy(r => r.RequestedAt)
                .Select(r => new
                {
                    r.Id,
                    r.AttendanceLogId,
                    r.InternId,
                    InternName = r.Intern != null
                        ? r.Intern.FullName
                        : r.AttendanceLog != null ? r.AttendanceLog.InternName : null,
                    InternEmail = r.Intern != null
                        ? r.Intern.Email
                        : r.AttendanceLog != null ? r.AttendanceLog.InternEmail : null,
                    Date = r.AttendanceLog != null ? (DateTime?)r.AttendanceLog.Date : null,
                    ShiftType = r.AttendanceLog != null ? r.AttendanceLog.ShiftType : null,
                    TimeIn = r.AttendanceLog != null ? (DateTime?)r.AttendanceLog.TimeIn : null,
                    TimeOut = r.AttendanceLog != null ? r.AttendanceLog.TimeOut : null,
                    r.RequestedTime,
                    r.TargetField,
                    r.Reason,
                    r.Status,
                    r.RequestedAt
                })
                .ToListAsync();

            var items = rows.Select(r => new
            {
                r.Id,
                r.AttendanceLogId,
                r.InternId,
                r.InternName,
                r.InternEmail,
                r.Date,
                r.ShiftType,
                r.TimeIn,
                r.TimeOut,
                CurrentTime = AttendanceAdjustmentField.IsTimeIn(r.TargetField) ? r.TimeIn : r.TimeOut,
                r.RequestedTime,
                r.TargetField,
                r.Reason,
                r.Status,
                r.RequestedAt
            });

            return Ok(items);
        }

        /// <summary>POST: api/HR/AttendanceRequests/Approve/{id}</summary>
        [HttpPost("AttendanceRequests/Approve/{id:int}")]
        public async Task<ActionResult<object>> ApproveAttendanceRequest(int id)
        {
            var request = await _context.AttendanceAdjustmentRequests
                .Include(r => r.AttendanceLog)
                .Include(r => r.Intern)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound(new { message = $"Attendance adjustment request {id} was not found." });
            }

            if (!string.Equals(request.Status, AttendanceAdjustmentStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = $"This request has already been {request.Status.ToLowerInvariant()}." });
            }

            var log = request.AttendanceLog;
            if (log is null)
            {
                return BadRequest(new { message = "The linked attendance log no longer exists." });
            }

            var field = AttendanceAdjustmentField.Normalize(request.TargetField);
            if (field is null)
            {
                return BadRequest(new { message = "The request target field is not valid." });
            }

            var logIsMorning = !string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase);
            if (AttendanceAdjustmentField.IsMorning(field) != logIsMorning)
            {
                return BadRequest(new { message = "The request's target field no longer matches the attendance log." });
            }

            var label = AttendanceAdjustmentField.Label(field);
            var approvedTime = log.Date.Date.Add(request.RequestedTime.TimeOfDay);
            var now = DateTime.Now;

            if (AttendanceAdjustmentField.IsTimeIn(field))
            {
                if (log.TimeOut is not null && approvedTime >= log.TimeOut.Value)
                {
                    return BadRequest(new { message = $"The requested {label} must be before the time out." });
                }

                log.TimeIn = approvedTime;
                log.Status = AttendanceShift.TimeInStatus(log.ShiftType, approvedTime);
            }
            else
            {
                if (approvedTime <= log.TimeIn)
                {
                    return BadRequest(new { message = $"The requested {label} must be after the time in." });
                }

                log.TimeOut = approvedTime;
            }

            log.UpdatedAt = now;
            request.Status = AttendanceAdjustmentStatus.Approved;
            request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewedAt = now;

            await NotifyInternAsync(
                request.Intern,
                "Time adjustment approved",
                $"Your {label} adjustment for {log.Date:MMM d, yyyy} was approved.",
                now);

            await _context.SaveChangesAsync();

            var dailyHours = await CalculateDailyHoursAsync(request.InternId, log.Date);

            return Ok(new
            {
                message = "Attendance adjustment approved.",
                request = new
                {
                    request.Id,
                    request.Status,
                    request.ReviewedAt,
                    request.TargetField
                },
                attendanceLog = new
                {
                    log.Id,
                    log.Date,
                    log.ShiftType,
                    log.TimeIn,
                    log.TimeOut,
                    log.Status
                },
                dailyHours
            });
        }

        /// <summary>POST: api/HR/AttendanceRequests/Reject/{id}</summary>
        [HttpPost("AttendanceRequests/Reject/{id:int}")]
        public async Task<ActionResult<object>> RejectAttendanceRequest(int id)
        {
            var request = await _context.AttendanceAdjustmentRequests
                .Include(r => r.AttendanceLog)
                .Include(r => r.Intern)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound(new { message = $"Attendance adjustment request {id} was not found." });
            }

            if (!string.Equals(request.Status, AttendanceAdjustmentStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = $"This request has already been {request.Status.ToLowerInvariant()}." });
            }

            var now = DateTime.Now;
            request.Status = AttendanceAdjustmentStatus.Rejected;
            request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewedAt = now;

            var label = AttendanceAdjustmentField.Label(request.TargetField);
            var when = request.AttendanceLog?.Date.ToString("MMM d, yyyy") ?? "the selected day";
            await NotifyInternAsync(
                request.Intern,
                "Time adjustment rejected",
                $"Your {label} adjustment for {when} was rejected.",
                now);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Attendance adjustment rejected.",
                request = new
                {
                    request.Id,
                    request.Status,
                    request.ReviewedAt
                }
            });
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

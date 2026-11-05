using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>GET: api/HR/AttendanceRequests/Pending</summary>
        [HttpGet("AttendanceRequests/Pending")]
        public async Task<ActionResult<IEnumerable<object>>> GetPendingAttendanceRequests()
        {
            var items = await _context.AttendanceAdjustmentRequests
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
                    Date = r.AttendanceLog != null ? r.AttendanceLog.Date : (DateTime?)null,
                    ShiftType = r.AttendanceLog != null ? r.AttendanceLog.ShiftType : null,
                    TimeIn = r.AttendanceLog != null ? r.AttendanceLog.TimeIn : (DateTime?)null,
                    CurrentTimeOut = r.AttendanceLog != null ? r.AttendanceLog.TimeOut : null,
                    r.RequestedTimeOut,
                    r.TargetShift,
                    r.Reason,
                    r.Status,
                    r.RequestedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>POST: api/HR/AttendanceRequests/Approve/{id}</summary>
        [HttpPost("AttendanceRequests/Approve/{id:int}")]
        public async Task<ActionResult<object>> ApproveAttendanceRequest(int id)
        {
            var request = await _context.AttendanceAdjustmentRequests
                .Include(r => r.AttendanceLog)
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

            var expectedShift = string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase)
                ? AttendanceAdjustmentShift.AfternoonOut
                : AttendanceAdjustmentShift.MorningOut;
            if (!string.Equals(request.TargetShift, expectedShift, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "The request's target shift no longer matches the attendance log." });
            }

            var approvedTimeOut = log.Date.Date.Add(request.RequestedTimeOut.TimeOfDay);
            if (approvedTimeOut <= log.TimeIn)
            {
                return BadRequest(new { message = "The requested time-out must be after the attendance time-in." });
            }

            var now = DateTime.Now;

            // DailyTimeRecord stores one row per shift, so the target field is
            // this log's TimeOut (the AM or PM time-out column in the UI).
            log.TimeOut = approvedTimeOut;
            log.UpdatedAt = now;

            request.Status = AttendanceAdjustmentStatus.Approved;
            request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewedAt = now;

            await _context.SaveChangesAsync();

            var dailyHours = await CalculateDailyHoursAsync(request.InternId, log.Date);

            return Ok(new
            {
                message = "Attendance adjustment approved.",
                request = new
                {
                    request.Id,
                    request.Status,
                    request.ReviewedAt
                },
                attendanceLog = new
                {
                    log.Id,
                    log.Date,
                    log.ShiftType,
                    log.TimeIn,
                    log.TimeOut
                },
                dailyHours
            });
        }

        /// <summary>POST: api/HR/AttendanceRequests/Reject/{id}</summary>
        [HttpPost("AttendanceRequests/Reject/{id:int}")]
        public async Task<ActionResult<object>> RejectAttendanceRequest(int id)
        {
            var request = await _context.AttendanceAdjustmentRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request is null)
            {
                return NotFound(new { message = $"Attendance adjustment request {id} was not found." });
            }

            if (!string.Equals(request.Status, AttendanceAdjustmentStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = $"This request has already been {request.Status.ToLowerInvariant()}." });
            }

            request.Status = AttendanceAdjustmentStatus.Rejected;
            request.ReviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.ReviewedAt = DateTime.Now;

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

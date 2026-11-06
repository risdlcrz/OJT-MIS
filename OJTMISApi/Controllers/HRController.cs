using System.Globalization;
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
                    RequestedTimeValue = r.RequestedTime,
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
                TimeIn = FormatTimestamp(r.TimeIn),
                TimeOut = FormatTimestamp(r.TimeOut),
                CurrentTime = FormatTimestamp(ResolveCurrentTime(r.TargetField, r.TimeIn, r.TimeOut)),
                RequestedTime = FormatTimestamp(r.RequestedTimeValue),
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

        /// <summary>
        /// Interns whose rendered attendance hours meet or exceed the required hours.
        /// </summary>
        [HttpGet("Certificates/Eligible")]
        public async Task<ActionResult<IEnumerable<object>>> GetCertificateEligible()
        {
            var interns = await _context.Interns
                .AsNoTracking()
                .Where(i => i.Status == "Hired" || i.Status == "Completed")
                .OrderBy(i => i.FullName)
                .ToListAsync();

            var punches = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(d => d.InternId != null && d.TimeOut != null)
                .Select(d => new { d.InternId, d.TimeIn, d.TimeOut })
                .ToListAsync();

            var hoursByIntern = punches
                .GroupBy(d => d.InternId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => Math.Round(g.Sum(d => Math.Max(0, (d.TimeOut!.Value - d.TimeIn).TotalHours)), 2));

            var eligible = interns
                .Select(intern =>
                {
                    hoursByIntern.TryGetValue(intern.Id, out var rendered);
                    return new
                    {
                        intern.Id,
                        intern.FullName,
                        intern.FirstName,
                        intern.MiddleName,
                        intern.LastName,
                        intern.Suffix,
                        intern.ApplicantNo,
                        intern.School,
                        intern.SchoolName,
                        intern.Program,
                        intern.ApplicantOffice,
                        intern.RequiredHours,
                        intern.StartDate,
                        intern.EndDate,
                        intern.Status,
                        HoursRendered = rendered
                    };
                })
                .Where(intern => intern.RequiredHours > 0 && intern.HoursRendered + 0.009 >= intern.RequiredHours)
                .ToList();

            return Ok(eligible);
        }

        /// <summary>POST: api/HR/Documents/Generated — records ID or certificate printing.</summary>
        [HttpPost("Documents/Generated")]
        public async Task<ActionResult<object>> LogGeneratedDocument([FromBody] GeneratedDocumentInput input)
        {
            var type = NormalizeGeneratedType(input.DocumentType);
            if (type is null)
            {
                return BadRequest(new { message = "Choose ID or Certificate of Completion." });
            }

            var internName = (input.InternName ?? string.Empty).Trim();
            if (input.InternId is int internId)
            {
                var intern = await _context.Interns.AsNoTracking().FirstOrDefaultAsync(i => i.Id == internId);
                if (intern is not null && string.IsNullOrWhiteSpace(internName))
                {
                    internName = intern.FullName;
                }
            }

            if (string.IsNullOrWhiteSpace(internName))
            {
                return BadRequest(new { message = "Intern name is required." });
            }

            var user = await _userManager.GetUserAsync(User);
            var action = type == "ID"
                ? "Generated an intern ID"
                : "Generated a Certificate of Completion";

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = user?.FullName ?? User.Identity?.Name ?? "HR Admin",
                Email = user?.Email ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Category = "HR",
                Method = "POST",
                Path = "/api/HR/Documents/Generated",
                Action = action,
                Details = $"{action} for {internName}.",
                IsVerified = true,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = $"{action} for {internName}." });
        }

        private static DateTime? ResolveCurrentTime(string? targetField, DateTime? timeIn, DateTime? timeOut)
        {
            var field = AttendanceAdjustmentField.Normalize(targetField);
            if (field is not null)
            {
                return AttendanceAdjustmentField.IsTimeIn(field) ? timeIn : timeOut;
            }

            var text = (targetField ?? string.Empty).Trim();
            if (text.Contains("in", StringComparison.OrdinalIgnoreCase)) return timeIn;
            if (text.Contains("out", StringComparison.OrdinalIgnoreCase)) return timeOut;
            return timeOut ?? timeIn;
        }

        /// <summary>
        /// Second-precision timestamp. Seven fractional digits are rejected by Date.
        /// </summary>
        private static string? FormatTimestamp(DateTime? value) =>
            value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

        private static string? NormalizeGeneratedType(string? value)
        {
            var type = (value ?? string.Empty).Trim();
            if (type.Equals("ID", StringComparison.OrdinalIgnoreCase)) return "ID";
            if (type.Equals("Certificate of Completion", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("COC", StringComparison.OrdinalIgnoreCase))
            {
                return "Certificate of Completion";
            }

            return null;
        }
    }

    public class GeneratedDocumentInput
    {
        public string? DocumentType { get; set; }
        public int? InternId { get; set; }
        public string? InternName { get; set; }
    }
}

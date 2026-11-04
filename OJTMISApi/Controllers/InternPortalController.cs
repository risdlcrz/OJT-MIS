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
    [Route("api/intern")]
    [Authorize(Roles = UserRoles.Intern)]
    [Produces("application/json")]
    public class InternPortalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AttendanceLogService _attendance;

        public InternPortalController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AttendanceLogService attendance)
        {
            _context = context;
            _userManager = userManager;
            _attendance = attendance;
        }

        [HttpGet("portal")]
        public async Task<ActionResult<object>> GetPortal()
        {
            var intern = await _attendance.ResolveInternAsync(User);
            if (intern is null) return ForbidIntern();

            var records = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == intern.Id)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.TimeIn)
                .ToListAsync();

            var documents = await _context.DocumentRequests
                .AsNoTracking()
                .Where(x => x.InternId == intern.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var rendered = records.Sum(x => AttendanceShift.Hours(x));

            var days = records
                .GroupBy(x => x.Date.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => AttendanceShift.ShapeDay(g.Key, g.ToList()))
                .Where(day => day is not null)
                .ToList();

            var todayRecords = records.Where(x => x.Date.Date == DateTime.Now.Date).ToList();

            return Ok(new
            {
                clockedIn = todayRecords.Any(x => x.TimeOut is null),
                intern = new
                {
                    intern.Id,
                    intern.FullName,
                    intern.Email,
                    intern.ApplicantNo,
                    intern.ApplicantOffice,
                    intern.SchoolName,
                    intern.Program,
                    intern.RequiredHours,
                    intern.StartDate,
                    intern.EndDate
                },
                hoursRendered = Math.Round(rendered, 2),
                remainingHours = Math.Round(Math.Max(0, intern.RequiredHours - rendered), 2),
                today = AttendanceShift.ShapeDay(DateTime.Now.Date, todayRecords),
                attendance = days,
                documents
            });
        }

        [HttpPost("dtr/scan")]
        public async Task<ActionResult<object>> Scan([FromBody] ScanRequest request)
        {
            var result = await _attendance.LogAsync(User, request.Payload);
            return StatusCode(result.Status, result.Body);
        }

        [HttpPost("documents")]
        public async Task<ActionResult<DocumentRequest>> RequestDocument([FromBody] DocumentRequestInput request)
        {
            var intern = await _attendance.ResolveInternAsync(User);
            if (intern is null) return ForbidIntern();

            var type = NormalizeType(request.DocumentType);
            if (type is null)
            {
                return BadRequest(new { message = "Choose ID or Certificate of Completion." });
            }

            var pending = await _context.DocumentRequests.AnyAsync(x =>
                x.InternId == intern.Id &&
                x.DocumentType == type &&
                x.Status == "Pending");
            if (pending)
            {
                return Conflict(new { message = $"You already have a pending {type} request." });
            }

            var now = DateTime.Now;
            var entity = new DocumentRequest
            {
                InternId = intern.Id,
                InternName = intern.FullName,
                InternEmail = intern.Email,
                DocumentType = type,
                Status = "Pending",
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.DocumentRequests.Add(entity);

            var hrUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HRAdmin);
            foreach (var hr in hrUsers)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = hr.Id,
                    Title = "Document request created",
                    Message = $"{intern.FullName} requested a {type}.",
                    Module = "Documents",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        private ObjectResult ForbidIntern() =>
            StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "This account is not linked to a hired intern record."
            });

        private static string? NormalizeType(string? value)
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

    public class ScanRequest
    {
        public string Payload { get; set; } = string.Empty;
    }

    public class DocumentRequestInput
    {
        public string DocumentType { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}

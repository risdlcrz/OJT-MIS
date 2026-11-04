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
    [Route("api/intern")]
    [Authorize(Roles = UserRoles.Intern)]
    [Produces("application/json")]
    public class InternPortalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InternPortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("portal")]
        public async Task<ActionResult<object>> GetPortal()
        {
            var intern = await ResolveInternAsync();
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

            var rendered = records
                .Where(x => x.TimeOut is not null)
                .Sum(x => Math.Max(0, (x.TimeOut!.Value - x.TimeIn).TotalHours));

            var today = records.FirstOrDefault(x => x.Date.Date == DateTime.Now.Date);

            return Ok(new
            {
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
                today,
                attendance = records,
                documents
            });
        }

        [HttpPost("dtr/scan")]
        public async Task<ActionResult<object>> Scan([FromBody] ScanRequest request)
        {
            var intern = await ResolveInternAsync();
            if (intern is null) return ForbidIntern();

            var payload = (request.Payload ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return BadRequest(new { message = "No ID was scanned." });
            }

            if (!MatchesInternId(intern, payload))
            {
                return BadRequest(new { message = "This ID does not match your intern record." });
            }

            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var existing = await _context.DailyTimeRecords
                .Where(x => x.InternId == intern.Id && x.Date >= today && x.Date < tomorrow)
                .OrderByDescending(x => x.TimeIn)
                .FirstOrDefaultAsync();

            if (existing is null)
            {
                var record = new DailyTimeRecord
                {
                    InternId = intern.Id,
                    InternName = intern.FullName,
                    InternEmail = intern.Email,
                    Date = today,
                    TimeIn = now,
                    Status = now.TimeOfDay > new TimeSpan(8, 30, 0) ? "Late" : "Present",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.DailyTimeRecords.Add(record);
                await _context.SaveChangesAsync();
                return Ok(new { action = "in", record });
            }

            if (existing.TimeOut is null)
            {
                existing.TimeOut = now;
                existing.UpdatedAt = now;
                await _context.SaveChangesAsync();
                return Ok(new { action = "out", record = existing });
            }

            return Conflict(new { message = "You already completed time in and time out for today." });
        }

        [HttpPost("documents")]
        public async Task<ActionResult<DocumentRequest>> RequestDocument([FromBody] DocumentRequestInput request)
        {
            var intern = await ResolveInternAsync();
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

        private async Task<Intern?> ResolveInternAsync()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? (await _userManager.GetUserAsync(User))?.Email;
            if (string.IsNullOrWhiteSpace(email)) return null;

            var key = email.Trim().ToLower();
            return await _context.Interns
                .Where(i => i.Email != null && i.Email.ToLower() == key && i.Status == "Hired")
                .OrderByDescending(i => i.EndDate)
                .FirstOrDefaultAsync();
        }

        private ObjectResult ForbidIntern() =>
            StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "This account is not linked to a hired intern record."
            });

        private static bool MatchesInternId(Intern intern, string payload)
        {
            var text = payload.Replace('\r', '\n');
            var needles = new[]
            {
                intern.ApplicantNo,
                intern.Id.ToString(),
                intern.Email,
                intern.FullName
            }.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim());

            return needles.Any(n => text.Contains(n, StringComparison.OrdinalIgnoreCase));
        }

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

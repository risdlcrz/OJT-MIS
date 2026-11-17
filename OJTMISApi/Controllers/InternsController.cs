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
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class InternsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InternsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccountProvisioningService _accounts;

        public InternsController(
            ApplicationDbContext context,
            ILogger<InternsController> logger,
            UserManager<ApplicationUser> userManager,
            AccountProvisioningService accounts)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _accounts = accounts;
        }

        /// <summary>GET: api/interns</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string? status)
        {
            try
            {
                var query = _context.Interns.AsNoTracking();

                // Hired interns stay on the list after they are marked Completed
                // so HR can open Generate COC from the row.
                query = string.IsNullOrWhiteSpace(status)
                    ? query.Where(x => x.Status == "Hired" || x.Status == "Completed")
                    : query.Where(x => x.Status == status);

                // Fetch interns first
                var interns = await query
                    .OrderBy(x => x.FullName)
                    .ToListAsync();

                // Fetch all schools for abbreviation lookup
                var schools = await _context.Schools
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        s => (s.Name ?? string.Empty).ToLower().Trim(),
                        s => s.Abbreviation ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase);

                // Combine interns with school abbreviations
                var items = interns.Select(intern => new
                {
                    intern.Id,
                    intern.FullName,
                    intern.School,
                    intern.SchoolName,
                    SchoolAbbreviation = schools.TryGetValue((intern.School ?? string.Empty).ToLower().Trim(), out var abbr) ? abbr : string.Empty,
                    intern.RequestId,
                    intern.Status,
                    intern.ApplicantId,
                    intern.ApplicantNo,
                    intern.FirstName,
                    intern.MiddleName,
                    intern.LastName,
                    intern.Suffix,
                    intern.Email,
                    intern.ContactNumber,
                    intern.HouseAddress,
                    intern.EducationLevel,
                    intern.Program,
                    intern.CoordName,
                    intern.RequiredHours,
                    intern.GuardianName,
                    intern.GuardianContact,
                    intern.RequirementsCsv,
                    intern.ApplicantOffice,
                    RequestOffice = intern.ApplicantOffice,
                    intern.Remarks,
                    intern.HireDate,
                    intern.DurationDays,
                    intern.ExcludeFriday,
                    intern.StartDate,
                    intern.EndDate,
                    intern.CreatedAt,
                    intern.UpdatedAt
                }).ToList();

                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Interns.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving Interns.", statusCode: 500);
            }
        }

        /// <summary>GET: api/interns/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetById(int id)
        {
            try
            {
                var item = await _context.Interns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = "Intern with id {id} was not found." });

                // Get school abbreviation
                var schoolAbbreviation = await _context.Schools
                    .Where(s => s.Name != null && s.Name.ToLower().Trim() == (item.School ?? string.Empty).ToLower().Trim())
                    .Select(s => s.Abbreviation)
                    .FirstOrDefaultAsync();

                var result = new
                {
                    item.Id,
                    item.FullName,
                    item.School,
                    item.SchoolName,
                    SchoolAbbreviation = schoolAbbreviation ?? string.Empty,
                    item.RequestId,
                    item.Status,
                    item.ApplicantId,
                    item.ApplicantNo,
                    item.FirstName,
                    item.MiddleName,
                    item.LastName,
                    item.Suffix,
                    item.Email,
                    item.ContactNumber,
                    item.HouseAddress,
                    item.EducationLevel,
                    item.Program,
                    item.CoordName,
                    item.RequiredHours,
                    item.GuardianName,
                    item.GuardianContact,
                    item.RequirementsCsv,
                    item.ApplicantOffice,
                    RequestOffice = item.ApplicantOffice,
                    item.Remarks,
                    item.HireDate,
                    item.DurationDays,
                    item.ExcludeFriday,
                    item.StartDate,
                    item.EndDate,
                    item.CreatedAt,
                    item.UpdatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Intern {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving the record.", statusCode: 500);
            }
        }

        /// <summary>POST: api/interns</summary>
        [HttpPost]
        public async Task<ActionResult<Intern>> Create([FromBody] Intern item)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.FullName?.Trim() ?? string.Empty;
            var f2 = item.School?.Trim() ?? string.Empty;

            if (await _context.Interns.AnyAsync(x => x.FullName != null && x.FullName.ToLower() == f1.ToLower()))
            {
                return Conflict(new { message = "A record with the same first field already exists." });
            }

            // Look up the school from the Schools table to ensure exact name match for abbreviation lookup
            var school = await _context.Schools
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name != null && 
                    s.Name.ToLower().Trim() == (f2 ?? string.Empty).ToLower().Trim());

            if (!string.IsNullOrEmpty(f2) && school is null)
            {
                return BadRequest(new { message = $"The school '{f2}' does not exist in the schools list. Please add it first." });
            }

            var now = AppClock.Now;
            var status = string.IsNullOrWhiteSpace(item.Status) ? "Applicant" : item.Status.Trim();
            var entity = new Intern
            {
                FullName = f1,
                School = school?.Name ?? f2,
                Email = item.Email?.Trim() ?? string.Empty,
                Status = status,
                RequestId = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                // Hiring needs a request that still has a slot.
                if (status == "Hired")
                {
                    if (item.RequestId is null)
                    {
                        return BadRequest(new { message = "Please select an intern request." });
                    }

                    var request = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == item.RequestId.Value);
                    if (request is null)
                    {
                        return BadRequest(new { message = "The selected intern request does not exist." });
                    }

                    if (!request.HasSlots)
                    {
                        return Conflict(new { message = $"Request from {request.OfficeName} has no available slots left." });
                    }

                    entity.RequestId = request.Id;
                    request.Filled += 1;
                    request.UpdatedAt = now;
                }

                _context.Interns.Add(entity);
                await _context.SaveChangesAsync();

                // List the intern in User Management right away (unverified).
                await ProvisionAccountAsync(entity);

                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Intern.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while creating the record.", statusCode: 500);
            }
        }

        /// <summary>PUT: api/interns/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Intern>> Update(int id, [FromBody] Intern item)
        {
            if (id != item.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.FullName?.Trim() ?? string.Empty;
            var f2 = item.School?.Trim() ?? string.Empty;

            try
            {
                var existing = await _context.Interns.FirstOrDefaultAsync(x => x.Id == id);
                if (existing is null) return NotFound(new { message = "Intern with id {id} was not found." });

                if (await _context.Interns.AnyAsync(x => x.Id != id && x.FullName != null && x.FullName.ToLower() == f1.ToLower()))
                {
                    return Conflict(new { message = "A record with the same first field already exists." });
                }

                // Look up the school from the Schools table to ensure exact name match for abbreviation lookup
                var school = await _context.Schools
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Name != null && 
                        s.Name.ToLower().Trim() == (f2 ?? string.Empty).ToLower().Trim());

                if (!string.IsNullOrEmpty(f2) && school is null)
                {
                    return BadRequest(new { message = $"The school '{f2}' does not exist in the schools list. Please add it first." });
                }

                existing.FullName = f1;
                existing.School = school?.Name ?? f2;
                existing.Email = item.Email?.Trim() ?? existing.Email;
                existing.UpdatedAt = AppClock.Now;
                await _context.SaveChangesAsync();

                await ProvisionAccountAsync(existing);
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Intern {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
        }

        /// <summary>
        /// Creates an unverified Intern account if there is none yet, so it
        /// shows up in User Management right away.
        /// </summary>
        private async Task ProvisionAccountAsync(Intern intern)
        {
            try
            {
                await _accounts.CreateAsync(
                    intern.Email,
                    string.IsNullOrWhiteSpace(intern.FirstName) ? intern.FullName : intern.FirstName,
                    intern.LastName ?? string.Empty,
                    intern.ApplicantOffice,
                    UserRoles.Intern);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not auto-create user account for intern {Name}.", intern.FullName);
            }
        }

        /// <summary>
        /// GET: api/interns/{id}/attendance-qr
        /// The code on the intern's current ID, or null when no ID has been
        /// generated yet. Reading it never changes it.
        /// </summary>
        [HttpGet("{id:int}/attendance-qr")]
        public async Task<ActionResult<object>> AttendanceQr(int id)
        {
            var intern = await _context.Interns.AsNoTracking()
                .Where(i => i.Id == id && i.Status == "Hired")
                .Select(i => new { i.AttendanceQrToken, i.AttendanceQrIssuedAt })
                .FirstOrDefaultAsync();
            if (intern is null)
            {
                return NotFound(new { message = "Hired intern was not found." });
            }

            return Ok(new { internId = id, payload = intern.AttendanceQrToken, issuedAt = intern.AttendanceQrIssuedAt });
        }

        /// <summary>
        /// POST: api/interns/{id}/attendance-qr
        /// Issues a brand-new unique code for a new ID card. The previous code
        /// (and every card printed with it) stops working immediately.
        /// </summary>
        [HttpPost("{id:int}/attendance-qr")]
        public async Task<ActionResult<object>> IssueAttendanceQr(int id, [FromServices] AttendanceQrService qr)
        {
            var intern = await _context.Interns.FirstOrDefaultAsync(i => i.Id == id && i.Status == "Hired");
            if (intern is null)
            {
                return NotFound(new { message = "Hired intern was not found." });
            }

            var replaced = !string.IsNullOrEmpty(intern.AttendanceQrToken);
            intern.AttendanceQrToken = qr.NewToken();
            intern.AttendanceQrIssuedAt = AppClock.Now;

            ActivityLogWriter.Add(
                _context,
                User,
                "POST",
                $"/api/interns/{id}/attendance-qr",
                replaced ? "Replaced an intern ID attendance code" : "Issued an intern ID attendance code",
                replaced
                    ? $"Issued a new attendance QR for {intern.FullName}; the previous ID card no longer works."
                    : $"Issued the first attendance QR for {intern.FullName}.");
            await _context.SaveChangesAsync();

            return Ok(new { internId = id, payload = intern.AttendanceQrToken, issuedAt = intern.AttendanceQrIssuedAt, replaced });
        }

        /// <summary>DELETE: api/interns/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.Interns.FindAsync(id);
                if (entity is null) return NotFound(new { message = "Intern with id {id} was not found." });

                _context.Interns.Remove(entity);

                // Give the slot back if the intern used one from a request.
                if (entity.RequestId is not null)
                {
                    var request = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == entity.RequestId.Value);
                    if (request is not null)
                    {
                        request.Filled = Math.Max(0, request.Filled - 1);
                        request.UpdatedAt = AppClock.Now;
                    }
                }

                // Also delete the linked Applicant record, so it does not stay
                // "Hired" with no intern (it would not show in either table).
                if (entity.ApplicantId is not null)
                {
                    var applicant = await _context.Applicants.FindAsync(entity.ApplicantId.Value);
                    if (applicant is not null)
                    {
                        _context.Applicants.Remove(applicant);
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Intern {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while deleting the record.", statusCode: 500);
            }
        }
    }
}



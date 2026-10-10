using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

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

        public InternsController(ApplicationDbContext context, ILogger<InternsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/interns</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] string? status)
        {
            try
            {
                var query = _context.Interns.AsNoTracking();

                // Ang Intern List ay para lamang sa mga naka-"Hired".
                query = string.IsNullOrWhiteSpace(status)
                    ? query.Where(x => x.Status == "Hired")
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

            var now = DateTime.Now;
            var status = string.IsNullOrWhiteSpace(item.Status) ? "Applicant" : item.Status.Trim();
            var entity = new Intern
            {
                FullName = f1,
                School = school?.Name ?? f2,
                Status = status,
                RequestId = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                // Kailangan ng request na may slot bago mag-hire.
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
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Intern {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
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

                // Ibalik ang slot kung may kinuha siya na request.
                if (entity.RequestId is not null)
                {
                    var request = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == entity.RequestId.Value);
                    if (request is not null)
                    {
                        request.Filled = Math.Max(0, request.Filled - 1);
                        request.UpdatedAt = DateTime.Now;
                    }
                }

                // I-delete din ang kaugnay na Applicant record para hindi ito
                // manatiling "Hired" na walang intern (invisible sa dalawang table).
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



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
    public class SchoolsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SchoolsController> _logger;

        public SchoolsController(ApplicationDbContext context, ILogger<SchoolsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Cascades school name updates to related Intern and Applicant records.
        /// School abbreviation is not stored in related entities; it's fetched dynamically from the Schools table.
        /// </summary>
        private async Task CascadeSchoolNameUpdateAsync(string oldName, string newName, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName) || 
                string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                // Update Interns with matching school name (case-insensitive)
                var interns = await _context.Interns
                    .Where(i => i.School != null && i.School.ToLower() == oldName.ToLower())
                    .ToListAsync();

                foreach (var intern in interns)
                {
                    intern.School = newName;
                    intern.SchoolName = newName;
                    intern.UpdatedAt = now;
                }

                // Update Applicants with matching school name (case-insensitive)
                var applicants = await _context.Applicants
                    .Where(a => a.SchoolName != null && a.SchoolName.ToLower() == oldName.ToLower())
                    .ToListAsync();

                foreach (var applicant in applicants)
                {
                    applicant.SchoolName = newName;
                    applicant.UpdatedAt = now;
                }

                if (interns.Count > 0 || applicants.Count > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cascaded school name update from '{OldName}' to '{NewName}' for {InternCount} interns and {ApplicantCount} applicants.",
                        oldName, newName, interns.Count, applicants.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cascading school update from '{OldName}' to '{NewName}'.", oldName, newName);
                // Don't throw - the school update succeeded, cascade is best effort
            }
        }

        /// <summary>
        /// GET: api/schools
        /// Retrieves all schools.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<School>>> GetAll()
        {
            try
            {
                var schools = await _context.Schools
                    .AsNoTracking()
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return Ok(schools);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message;
                var detail = $"Error: {ex.Message}" + (inner != null ? $"\nInner: {inner}" : "");
                _logger.LogError(ex, "Error retrieving schools. Detail: {Detail}", detail);
                Console.WriteLine($"[SchoolsController.GetAll] ERROR: {detail}");
                return Problem(
                    title: "Internal Server Error",
                    detail: detail,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// GET: api/schools/5
        /// Retrieves a single school by its identifier.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<School>> GetById(int id)
        {
            try
            {
                var school = await _context.Schools
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (school is null)
                {
                    return NotFound(new { message = $"School with id {id} was not found." });
                }

                return Ok(school);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving school {Id}.", id);
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while retrieving the school.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// POST: api/schools
        /// Creates a new school.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<School>> Create([FromBody] School school)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var name = school.Name?.Trim() ?? string.Empty;
            var abbreviation = school.Abbreviation?.Trim() ?? string.Empty;
            var address = school.Address?.Trim() ?? string.Empty;
            var moaStatus = school.MoaStatus;
            var moaExpiry = school.MoaExpiry;

            var nameExists = await _context.Schools
                .AnyAsync(s => s.Name != null && s.Name.ToLower() == name.ToLower());

            if (nameExists)
            {
                return Conflict(new { message = $"A school named \"{name}\" already exists." });
            }

            var now = DateTime.Now;
            var newSchool = new School
            {
                Name = name,
                Abbreviation = abbreviation,
                Address = address,
                MoaStatus = moaStatus,
                MoaExpiry = moaExpiry,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _context.Schools.Add(newSchool);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = newSchool.Id }, newSchool);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error creating school {Name}.", name);
                return Conflict(new { message = $"A school named \"{name}\" already exists." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating school.");
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while creating the school.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// PUT: api/schools/5
        /// Updates an existing school.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<School>> Update(int id, [FromBody] School school)
        {
            if (id != school.Id)
            {
                return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var name = school.Name?.Trim() ?? string.Empty;
            var abbreviation = school.Abbreviation?.Trim() ?? string.Empty;
            var address = school.Address?.Trim() ?? string.Empty;
            var moaStatus = school.MoaStatus;
            var moaExpiry = school.MoaExpiry;

            try
            {
                var existing = await _context.Schools.FirstOrDefaultAsync(s => s.Id == id);
                if (existing is null)
                {
                    return NotFound(new { message = $"School with id {id} was not found." });
                }

                var nameExists = await _context.Schools
                    .AnyAsync(s => s.Id != id && s.Name != null && s.Name.ToLower() == name.ToLower());

                if (nameExists)
                {
                    return Conflict(new { message = $"A school named \"{name}\" already exists." });
                }

                // Store old name for cascade update
                var oldName = existing.Name;

                existing.Name = name;
                existing.Abbreviation = abbreviation;
                existing.Address = address;
                existing.MoaStatus = moaStatus;
                existing.MoaExpiry = moaExpiry;
                var now = DateTime.Now;
                existing.UpdatedAt = now;

                await _context.SaveChangesAsync();

                // Cascade update to related records if name changed
                if (!string.Equals(oldName, name, StringComparison.OrdinalIgnoreCase))
                {
                    await CascadeSchoolNameUpdateAsync(oldName, name, now);
                }

                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating school {Id}.", id);
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while updating the school.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// DELETE: api/schools/5
        /// Deletes a school.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var school = await _context.Schools.FindAsync(id);
                if (school is null)
                {
                    return NotFound(new { message = $"School with id {id} was not found." });
                }

                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting school {Id}.", id);
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while deleting the school.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}



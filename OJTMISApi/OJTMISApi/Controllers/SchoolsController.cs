using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchoolsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SchoolsController> _logger;

        public SchoolsController(ApplicationDbContext context, ILogger<SchoolsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/schools
        [HttpGet]
        public async Task<ActionResult<IEnumerable<School>>> GetSchools()
        {
            try
            {
                var schools = await _context.Schools.ToListAsync();
                return Ok(schools);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching schools: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/schools/5
        [HttpGet("{id}")]
        public async Task<ActionResult<School>> GetSchool(int id)
        {
            try
            {
                var school = await _context.Schools.FindAsync(id);
                if (school == null)
                {
                    return NotFound();
                }
                return Ok(school);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching school {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/schools
        [HttpPost]
        public async Task<ActionResult<School>> CreateSchool(School school)
        {
            try
            {
                school.DateCreated = DateTime.UtcNow;
                school.DateModified = DateTime.UtcNow;

                _context.Schools.Add(school);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSchool), new { id = school.Id }, school);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating school: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/schools/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSchool(int id, School school)
        {
            if (id != school.Id)
            {
                return BadRequest();
            }

            try
            {
                school.DateModified = DateTime.UtcNow;
                _context.Entry(school).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SchoolExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating school {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/schools/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchool(int id)
        {
            try
            {
                var school = await _context.Schools.FindAsync(id);
                if (school == null)
                {
                    return NotFound();
                }

                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting school {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool SchoolExists(int id)
        {
            return _context.Schools.Any(e => e.Id == id);
        }
    }
}

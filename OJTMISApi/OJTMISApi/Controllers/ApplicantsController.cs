using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicantsController> _logger;

        public ApplicantsController(ApplicationDbContext context, ILogger<ApplicantsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/applicants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Applicant>>> GetApplicants()
        {
            try
            {
                var applicants = await _context.Applicants.ToListAsync();
                return Ok(applicants);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching applicants: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/applicants/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Applicant>> GetApplicant(int id)
        {
            try
            {
                var applicant = await _context.Applicants.FindAsync(id);
                if (applicant == null)
                {
                    return NotFound();
                }
                return Ok(applicant);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching applicant {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/applicants
        [HttpPost]
        public async Task<ActionResult<Applicant>> CreateApplicant(Applicant applicant)
        {
            try
            {
                applicant.DateCreated = DateTime.UtcNow;
                applicant.DateModified = DateTime.UtcNow;

                _context.Applicants.Add(applicant);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetApplicant), new { id = applicant.Id }, applicant);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating applicant: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/applicants/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApplicant(int id, Applicant applicant)
        {
            if (id != applicant.Id)
            {
                return BadRequest();
            }

            try
            {
                applicant.DateModified = DateTime.UtcNow;
                _context.Entry(applicant).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApplicantExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating applicant {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/applicants/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApplicant(int id)
        {
            try
            {
                var applicant = await _context.Applicants.FindAsync(id);
                if (applicant == null)
                {
                    return NotFound();
                }

                _context.Applicants.Remove(applicant);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting applicant {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool ApplicantExists(int id)
        {
            return _context.Applicants.Any(e => e.Id == id);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InternsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InternsController> _logger;

        public InternsController(ApplicationDbContext context, ILogger<InternsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/interns
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Intern>>> GetInterns()
        {
            try
            {
                var interns = await _context.Interns.ToListAsync();
                return Ok(interns);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching interns: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/interns/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Intern>> GetIntern(int id)
        {
            try
            {
                var intern = await _context.Interns.FindAsync(id);
                if (intern == null)
                {
                    return NotFound();
                }
                return Ok(intern);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching intern {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/interns
        [HttpPost]
        public async Task<ActionResult<Intern>> CreateIntern(Intern intern)
        {
            try
            {
                intern.DateCreated = DateTime.UtcNow;
                intern.DateModified = DateTime.UtcNow;

                _context.Interns.Add(intern);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetIntern), new { id = intern.Id }, intern);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating intern: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/interns/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateIntern(int id, Intern intern)
        {
            if (id != intern.Id)
            {
                return BadRequest();
            }

            try
            {
                intern.DateModified = DateTime.UtcNow;
                _context.Entry(intern).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InternExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating intern {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/interns/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteIntern(int id)
        {
            try
            {
                var intern = await _context.Interns.FindAsync(id);
                if (intern == null)
                {
                    return NotFound();
                }

                _context.Interns.Remove(intern);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting intern {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool InternExists(int id)
        {
            return _context.Interns.Any(e => e.Id == id);
        }
    }
}

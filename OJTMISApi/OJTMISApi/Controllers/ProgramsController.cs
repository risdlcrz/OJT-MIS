using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProgramsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProgramsController> _logger;

        public ProgramsController(ApplicationDbContext context, ILogger<ProgramsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/programs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OJTProgram>>> GetPrograms()
        {
            try
            {
                var programs = await _context.Programs.ToListAsync();
                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching programs: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/programs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OJTProgram>> GetProgram(int id)
        {
            try
            {
                var program = await _context.Programs.FindAsync(id);
                if (program == null)
                {
                    return NotFound();
                }
                return Ok(program);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching program {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/programs
        [HttpPost]
        public async Task<ActionResult<OJTProgram>> CreateProgram(OJTProgram program)
        {
            try
            {
                program.DateCreated = DateTime.UtcNow;
                program.DateModified = DateTime.UtcNow;

                _context.Programs.Add(program);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetProgram), new { id = program.Id }, program);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating program: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/programs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProgram(int id, OJTProgram program)
        {
            if (id != program.Id)
            {
                return BadRequest();
            }

            try
            {
                program.DateModified = DateTime.UtcNow;
                _context.Entry(program).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProgramExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating program {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/programs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProgram(int id)
        {
            try
            {
                var program = await _context.Programs.FindAsync(id);
                if (program == null)
                {
                    return NotFound();
                }

                _context.Programs.Remove(program);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting program {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool ProgramExists(int id)
        {
            return _context.Programs.Any(e => e.Id == id);
        }
    }
}

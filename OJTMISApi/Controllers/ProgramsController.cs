using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProgramsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProgramsController> _logger;

        public ProgramsController(ApplicationDbContext context, ILogger<ProgramsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/programs</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OJTProgram>>> GetAll()
        {
            try
            {
                var items = await _context.Programs
                    .AsNoTracking()
                    .OrderBy(x => x.ProgramCode)
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Programs.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving Programs.", statusCode: 500);
            }
        }

        /// <summary>GET: api/programs/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OJTProgram>> GetById(int id)
        {
            try
            {
                var item = await _context.Programs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = "Program with id {id} was not found." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Program {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving the record.", statusCode: 500);
            }
        }

        /// <summary>POST: api/programs</summary>
        [HttpPost]
        public async Task<ActionResult<OJTProgram>> Create([FromBody] OJTProgram item)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.ProgramCode?.Trim() ?? string.Empty;
            var f2 = item.ProgramName?.Trim() ?? string.Empty;

            if (await _context.Programs.AnyAsync(x => x.ProgramCode != null && x.ProgramCode.ToLower() == f1.ToLower()))
            {
                return Conflict(new { message = "A record with the same first field already exists." });
            }

            var now = DateTime.Now;
            var entity = new OJTProgram { ProgramCode = f1, ProgramName = f2, CreatedAt = now, UpdatedAt = now };

            try
            {
                _context.Programs.Add(entity);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Program.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while creating the record.", statusCode: 500);
            }
        }

        /// <summary>PUT: api/programs/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<OJTProgram>> Update(int id, [FromBody] OJTProgram item)
        {
            if (id != item.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.ProgramCode?.Trim() ?? string.Empty;
            var f2 = item.ProgramName?.Trim() ?? string.Empty;

            try
            {
                var existing = await _context.Programs.FirstOrDefaultAsync(x => x.Id == id);
                if (existing is null) return NotFound(new { message = "Program with id {id} was not found." });

                if (await _context.Programs.AnyAsync(x => x.Id != id && x.ProgramCode != null && x.ProgramCode.ToLower() == f1.ToLower()))
                {
                    return Conflict(new { message = "A record with the same first field already exists." });
                }

                existing.ProgramCode = f1;
                existing.ProgramName = f2;
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Program {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
        }

        /// <summary>DELETE: api/programs/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.Programs.FindAsync(id);
                if (entity is null) return NotFound(new { message = "Program with id {id} was not found." });

                _context.Programs.Remove(entity);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Program {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while deleting the record.", statusCode: 500);
            }
        }
    }
}



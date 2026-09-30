using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        public async Task<ActionResult<IEnumerable<Intern>>> GetAll()
        {
            try
            {
                var items = await _context.Interns
                    .AsNoTracking()
                    .OrderBy(x => x.FullName)
                    .ToListAsync();
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
        public async Task<ActionResult<Intern>> GetById(int id)
        {
            try
            {
                var item = await _context.Interns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = "Intern with id {id} was not found." });
                return Ok(item);
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

            var now = DateTime.Now;
            var entity = new Intern { FullName = f1, School = f2, CreatedAt = now, UpdatedAt = now };

            try
            {
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

                existing.FullName = f1;
                existing.School = f2;
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


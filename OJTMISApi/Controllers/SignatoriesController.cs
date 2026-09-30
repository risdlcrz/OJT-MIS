using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SignatoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SignatoriesController> _logger;

        public SignatoriesController(ApplicationDbContext context, ILogger<SignatoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/signatories</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Signatory>>> GetAll()
        {
            try
            {
                var items = await _context.Signatories
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Signatories.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving Signatories.", statusCode: 500);
            }
        }

        /// <summary>GET: api/signatories/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Signatory>> GetById(int id)
        {
            try
            {
                var item = await _context.Signatories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = "Signatory with id {id} was not found." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving the record.", statusCode: 500);
            }
        }

        /// <summary>POST: api/signatories</summary>
        [HttpPost]
        public async Task<ActionResult<Signatory>> Create([FromBody] Signatory item)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.Name?.Trim() ?? string.Empty;
            var f2 = item.Position?.Trim() ?? string.Empty;

            if (await _context.Signatories.AnyAsync(x => x.Name != null && x.Name.ToLower() == f1.ToLower()))
            {
                return Conflict(new { message = "A record with the same first field already exists." });
            }

            var now = DateTime.Now;
            var entity = new Signatory { Name = f1, Position = f2, CreatedAt = now, UpdatedAt = now };

            try
            {
                _context.Signatories.Add(entity);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Signatory.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while creating the record.", statusCode: 500);
            }
        }

        /// <summary>PUT: api/signatories/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Signatory>> Update(int id, [FromBody] Signatory item)
        {
            if (id != item.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.Name?.Trim() ?? string.Empty;
            var f2 = item.Position?.Trim() ?? string.Empty;

            try
            {
                var existing = await _context.Signatories.FirstOrDefaultAsync(x => x.Id == id);
                if (existing is null) return NotFound(new { message = "Signatory with id {id} was not found." });

                if (await _context.Signatories.AnyAsync(x => x.Id != id && x.Name != null && x.Name.ToLower() == f1.ToLower()))
                {
                    return Conflict(new { message = "A record with the same first field already exists." });
                }

                existing.Name = f1;
                existing.Position = f2;
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
        }

        /// <summary>DELETE: api/signatories/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.Signatories.FindAsync(id);
                if (entity is null) return NotFound(new { message = "Signatory with id {id} was not found." });

                _context.Signatories.Remove(entity);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while deleting the record.", statusCode: 500);
            }
        }
    }
}


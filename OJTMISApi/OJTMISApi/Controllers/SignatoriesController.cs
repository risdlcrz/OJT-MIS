using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SignatoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SignatoriesController> _logger;

        public SignatoriesController(ApplicationDbContext context, ILogger<SignatoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/signatories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Signatory>>> GetSignatories()
        {
            try
            {
                var signatories = await _context.Signatories.ToListAsync();
                return Ok(signatories);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching signatories: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/signatories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Signatory>> GetSignatory(int id)
        {
            try
            {
                var signatory = await _context.Signatories.FindAsync(id);
                if (signatory == null)
                {
                    return NotFound();
                }
                return Ok(signatory);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching signatory {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/signatories
        [HttpPost]
        public async Task<ActionResult<Signatory>> CreateSignatory(Signatory signatory)
        {
            try
            {
                signatory.DateCreated = DateTime.UtcNow;
                signatory.DateModified = DateTime.UtcNow;

                _context.Signatories.Add(signatory);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetSignatory), new { id = signatory.Id }, signatory);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating signatory: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/signatories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSignatory(int id, Signatory signatory)
        {
            if (id != signatory.Id)
            {
                return BadRequest();
            }

            try
            {
                signatory.DateModified = DateTime.UtcNow;
                _context.Entry(signatory).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SignatoryExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating signatory {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/signatories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSignatory(int id)
        {
            try
            {
                var signatory = await _context.Signatories.FindAsync(id);
                if (signatory == null)
                {
                    return NotFound();
                }

                _context.Signatories.Remove(signatory);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting signatory {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool SignatoryExists(int id)
        {
            return _context.Signatories.Any(e => e.Id == id);
        }
    }
}

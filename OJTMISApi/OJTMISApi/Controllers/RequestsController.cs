using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(ApplicationDbContext context, ILogger<RequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/requests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InternRequest>>> GetRequests()
        {
            try
            {
                var requests = await _context.InternRequests.ToListAsync();
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching requests: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/requests/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InternRequest>> GetRequest(int id)
        {
            try
            {
                var request = await _context.InternRequests.FindAsync(id);
                if (request == null)
                {
                    return NotFound();
                }
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching request {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/requests
        [HttpPost]
        public async Task<ActionResult<InternRequest>> CreateRequest(InternRequest request)
        {
            try
            {
                request.DateCreated = DateTime.UtcNow;
                request.DateModified = DateTime.UtcNow;

                _context.InternRequests.Add(request);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRequest), new { id = request.Id }, request);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating request: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/requests/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(int id, InternRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            try
            {
                request.DateModified = DateTime.UtcNow;
                _context.Entry(request).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating request {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/requests/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _context.InternRequests.FindAsync(id);
                if (request == null)
                {
                    return NotFound();
                }

                _context.InternRequests.Remove(request);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting request {id}: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private bool RequestExists(int id)
        {
            return _context.InternRequests.Any(e => e.Id == id);
        }
    }
}

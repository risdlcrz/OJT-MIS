using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [Authorize(Roles = UserRoles.HRAdmin)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InternRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InternRequestsController> _logger;

        public InternRequestsController(ApplicationDbContext context, ILogger<InternRequestsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/internrequests</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InternRequest>>> GetAll()
        {
            var requests = await _context.InternRequests
                .AsNoTracking()
                .OrderBy(r => r.OfficeName)
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// GET: api/internrequests/available
        /// Para sa dropdown ng "hire". Kasama lamang ang mga request na
        /// Open AT may natitirang slot. Kapag wala nang slot, hindi na ito
        /// lumalabas para hindi ma-pili ng HR.
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<InternRequest>>> GetAvailable()
        {
            var requests = await _context.InternRequests
                .AsNoTracking()
                .Where(r => r.Status == "Open" && r.Filled < r.Count)
                .OrderBy(r => r.OfficeName)
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>GET: api/internrequests/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<InternRequest>> GetById(int id)
        {
            var request = await _context.InternRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound(new { message = $"Request with id {id} was not found." });
            return Ok(request);
        }

        /// <summary>POST: api/internrequests</summary>
        [HttpPost]
        public async Task<ActionResult<InternRequest>> Create([FromBody] InternRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var now = DateTime.Now;
            var entity = new InternRequest
            {
                OfficeCode = request.OfficeCode?.Trim() ?? string.Empty,
                OfficeName = request.OfficeName?.Trim() ?? string.Empty,
                Count = request.Count,
                Filled = 0,
                Skills = request.Skills?.Trim() ?? string.Empty,
                Description = request.Description?.Trim() ?? string.Empty,
                Status = "Open",
                SignatoryId = request.SignatoryId,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.InternRequests.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        /// <summary>PUT: api/internrequests/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<InternRequest>> Update(int id, [FromBody] InternRequest request)
        {
            if (id != request.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var existing = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (existing is null) return NotFound(new { message = $"Request with id {id} was not found." });

            if (request.Count < existing.Filled)
            {
                return BadRequest(new { message = $"Cannot set slots to {request.Count} because {existing.Filled} intern(s) are already hired from this request." });
            }

            existing.OfficeCode = request.OfficeCode?.Trim() ?? string.Empty;
            existing.OfficeName = request.OfficeName?.Trim() ?? string.Empty;
            existing.Count = request.Count;
            existing.Skills = request.Skills?.Trim() ?? string.Empty;
            existing.Description = request.Description?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(request.Status)) existing.Status = request.Status.Trim();
            existing.SignatoryId = request.SignatoryId;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>DELETE: api/internrequests/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.InternRequests.FindAsync(id);
            if (request is null) return NotFound(new { message = $"Request with id {id} was not found." });

            var hired = await _context.Interns.AnyAsync(i => i.RequestId == id && i.Status == "Hired");
            if (hired)
            {
                return Conflict(new { message = "Cannot delete a request that already has hired interns." });
            }

            _context.InternRequests.Remove(request);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

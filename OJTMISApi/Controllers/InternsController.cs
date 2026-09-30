using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.HRAdmin)]
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
        public async Task<ActionResult<IEnumerable<Intern>>> GetAll([FromQuery] string? status)
        {
            try
            {
                var query = _context.Interns.AsNoTracking();

                // Ang Intern List ay para lamang sa mga naka-"Hired".
                query = string.IsNullOrWhiteSpace(status)
                    ? query.Where(x => x.Status == "Hired")
                    : query.Where(x => x.Status == status);

                var items = await query
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
            var status = string.IsNullOrWhiteSpace(item.Status) ? "Applicant" : item.Status.Trim();
            var entity = new Intern
            {
                FullName = f1,
                School = f2,
                Status = status,
                RequestId = null,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                // Kailangan ng request na may slot bago mag-hire.
                if (status == "Hired")
                {
                    if (item.RequestId is null)
                    {
                        return BadRequest(new { message = "Please select an intern request." });
                    }

                    var request = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == item.RequestId.Value);
                    if (request is null)
                    {
                        return BadRequest(new { message = "The selected intern request does not exist." });
                    }

                    if (!request.HasSlots)
                    {
                        return Conflict(new { message = $"Request from {request.OfficeName} has no available slots left." });
                    }

                    entity.RequestId = request.Id;
                    request.Filled += 1;
                    request.UpdatedAt = now;
                }

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

                // Ibalik ang slot kung may kinuha siya na request.
                if (entity.RequestId is not null)
                {
                    var request = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == entity.RequestId.Value);
                    if (request is not null)
                    {
                        request.Filled = Math.Max(0, request.Filled - 1);
                        request.UpdatedAt = DateTime.Now;
                    }
                }

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



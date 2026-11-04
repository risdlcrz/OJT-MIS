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
    public class DtrController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DtrController> _logger;

        public DtrController(ApplicationDbContext context, ILogger<DtrController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/dtr</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DailyTimeRecord>>> GetAll()
        {
            try
            {
                var items = await _context.DailyTimeRecords
                    .AsNoTracking()
                    .OrderByDescending(x => x.Date)
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Daily Time Records.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving Daily Time Records.", statusCode: 500);
            }
        }

        /// <summary>GET: api/dtr/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DailyTimeRecord>> GetById(int id)
        {
            try
            {
                var item = await _context.DailyTimeRecords.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = $"Daily Time Record with id {id} was not found." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Daily Time Record {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving the record.", statusCode: 500);
            }
        }

        /// <summary>POST: api/dtr</summary>
        [HttpPost]
        public async Task<ActionResult<DailyTimeRecord>> Create([FromBody] DailyTimeRecord item)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var now = DateTime.Now;
            var entity = new DailyTimeRecord
            {
                Date = item.Date,
                ShiftType = string.IsNullOrWhiteSpace(item.ShiftType)
                    ? AttendanceShift.Morning
                    : item.ShiftType.Trim(),
                TimeIn = item.TimeIn,
                TimeOut = item.TimeOut,
                Status = string.IsNullOrWhiteSpace(item.Status)
                    ? AttendanceShift.TimeInStatus(
                        string.IsNullOrWhiteSpace(item.ShiftType) ? AttendanceShift.Morning : item.ShiftType,
                        item.TimeIn)
                    : item.Status,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _context.DailyTimeRecords.Add(entity);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Daily Time Record.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while creating the record.", statusCode: 500);
            }
        }

        /// <summary>PUT: api/dtr/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<DailyTimeRecord>> Update(int id, [FromBody] DailyTimeRecord item)
        {
            if (id != item.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            try
            {
                var existing = await _context.DailyTimeRecords.FirstOrDefaultAsync(x => x.Id == id);
                if (existing is null) return NotFound(new { message = $"Daily Time Record with id {id} was not found." });

                existing.Date = item.Date;
                existing.ShiftType = string.IsNullOrWhiteSpace(item.ShiftType)
                    ? AttendanceShift.Morning
                    : item.ShiftType.Trim();
                existing.TimeIn = item.TimeIn;
                existing.TimeOut = item.TimeOut;
                existing.Status = string.IsNullOrWhiteSpace(item.Status)
                    ? AttendanceShift.TimeInStatus(existing.ShiftType, item.TimeIn)
                    : item.Status;
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Daily Time Record {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
        }

        /// <summary>DELETE: api/dtr/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.DailyTimeRecords.FindAsync(id);
                if (entity is null) return NotFound(new { message = $"Daily Time Record with id {id} was not found." });

                _context.DailyTimeRecords.Remove(entity);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Daily Time Record {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while deleting the record.", statusCode: 500);
            }
        }
    }
}

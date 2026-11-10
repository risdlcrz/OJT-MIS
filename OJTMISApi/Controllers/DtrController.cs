using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

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
        private readonly InternCompletionService _completion;

        public DtrController(
            ApplicationDbContext context,
            ILogger<DtrController> logger,
            InternCompletionService completion)
        {
            _context = context;
            _logger = logger;
            _completion = completion;
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

                var missingIds = items
                    .Where(x => string.IsNullOrWhiteSpace(x.InternName) && x.InternId != null)
                    .Select(x => x.InternId!.Value)
                    .Distinct()
                    .ToList();

                if (missingIds.Count > 0)
                {
                    var names = await _context.Interns
                        .AsNoTracking()
                        .Where(i => missingIds.Contains(i.Id))
                        .ToDictionaryAsync(i => i.Id, i => i.FullName);

                    foreach (var item in items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.InternName) || item.InternId is not int id) continue;
                        if (names.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                        {
                            item.InternName = name;
                        }
                    }
                }

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
                InternId = item.InternId,
                InternName = item.InternName,
                InternEmail = item.InternEmail,
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

                // Kapag nagdagdag ng oras ang record na ito at naabot na ng
                // intern ang required hours, i-mark siyang Completed at
                // abisuhan ang HR (one-time).
                if (entity.InternId is int completedInternId)
                {
                    await _completion.CheckAsync(completedInternId);
                }

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

                // Kapag binago ang oras ng DTR at naabot (o lumampas) na ng
                // intern ang required hours, i-mark siyang Completed at
                // abisuhan ang HR (one-time).
                if (existing.InternId is int completedInternId)
                {
                    await _completion.CheckAsync(completedInternId);
                }

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

                var internId = entity.InternId;
                _context.DailyTimeRecords.Remove(entity);
                await _context.SaveChangesAsync();

                // Nawalan ng oras ang intern dahil sa natanggal na record;
                // i-refresh ang HoursRendered at tingnan kung may bagong
                // completion (hal. kapag binura ang maling entry).
                if (internId is int completedInternId)
                {
                    await _completion.CheckAsync(completedInternId);
                }

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

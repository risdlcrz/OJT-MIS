using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>GET: api/notifications?take=20</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetAll([FromQuery] int take = 20)
        {
            var userId = UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Session is no longer valid." });
            }

            take = Math.Clamp(take, 1, 100);

            var items = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(take)
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>PATCH: api/notifications/{id}/read</summary>
        [HttpPatch("{id:int}/read")]
        public async Task<ActionResult<Notification>> MarkRead(int id)
        {
            var userId = UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Session is no longer valid." });
            }

            var item = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (item is null)
            {
                return NotFound(new { message = $"Notification with id {id} was not found." });
            }

            if (!item.IsRead)
            {
                item.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(item);
        }

        /// <summary>PATCH: api/notifications/read-all</summary>
        [HttpPatch("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Session is no longer valid." });
            }

            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return NoContent();
        }

        /// <summary>DELETE: api/notifications (clear all)</summary>
        [HttpDelete]
        public async Task<IActionResult> Clear()
        {
            var userId = UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "Session is no longer valid." });
            }

            await _context.Notifications
                .Where(n => n.UserId == userId)
                .ExecuteDeleteAsync();

            return NoContent();
        }
    }
}

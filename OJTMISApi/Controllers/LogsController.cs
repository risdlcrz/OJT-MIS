using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/logs")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class LogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>GET: api/logs?category=HR&amp;search=user&amp;take=500</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? search,
            [FromQuery] int take = 500)
        {
            var query = _context.ActivityLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) &&
                !string.Equals(category, "all", StringComparison.OrdinalIgnoreCase))
            {
                var normalized = category.Trim();
                query = query.Where(l => l.Category == normalized);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l =>
                    l.UserName.ToLower().Contains(term) ||
                    l.Email.ToLower().Contains(term) ||
                    l.Action.ToLower().Contains(term));
            }

            take = Math.Clamp(take, 1, 2000);

            // Internal request details (method, path, status) are kept in the
            // database for auditing but are never exposed to the client.
            var items = await query
                .OrderByDescending(l => l.CreatedAt)
                .ThenByDescending(l => l.Id)
                .Take(take)
                .Select(l => new
                {
                    l.Id,
                    l.UserName,
                    l.Email,
                    l.Category,
                    l.Action,
                    Details = l.Details != null && l.Details.StartsWith("HTTP ")
                        ? "Action recorded."
                        : l.Details,
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}

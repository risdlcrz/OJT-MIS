using Microsoft.AspNetCore.Mvc;
using OJTMISApi.Data;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var stats = new
                {
                    totalApplicants = _context.Applicants.Count(),
                    totalPrograms = _context.Programs.Count(),
                    totalSchools = _context.Schools.Count(),
                    totalInterns = _context.Interns.Count(),
                    pendingRequests = _context.InternRequests.Count(r => r.Status == "Pending"),
                    approvedRequests = _context.InternRequests.Count(r => r.Status == "Approved")
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching dashboard stats: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

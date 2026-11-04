using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.Intern)]
    [Produces("application/json")]
    public class AttendanceController : ControllerBase
    {
        private readonly AttendanceLogService _logs;

        public AttendanceController(AttendanceLogService logs)
        {
            _logs = logs;
        }

        /// <summary>POST: api/Attendance/LogTime</summary>
        [HttpPost("LogTime")]
        public async Task<ActionResult<object>> LogTime([FromBody] ScanRequest request)
        {
            var result = await _logs.LogAsync(User, request.Payload);
            return StatusCode(result.Status, result.Body);
        }
    }
}

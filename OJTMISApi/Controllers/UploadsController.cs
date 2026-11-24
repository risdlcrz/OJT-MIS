using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    /// <summary>
    /// Upload limits for the Vue client, so the file pickers check (and show)
    /// exactly what the API enforces.
    /// </summary>
    [ApiController]
    [Route("api/uploads")]
    [Authorize]
    [Produces("application/json")]
    public class UploadsController : ControllerBase
    {
        private readonly UploadOptions _options;

        public UploadsController(IOptions<UploadOptions> options) => _options = options.Value;

        /// <summary>GET: api/uploads/policy</summary>
        [HttpGet("policy")]
        public ActionResult<object> GetPolicy()
        {
            return Ok(_options.All().ToDictionary(
                entry => char.ToLowerInvariant(entry.Name[0]) + entry.Name[1..],
                entry => new
                {
                    entry.Rule.MaxBytes,
                    entry.Rule.MaxFiles,
                    AllowedContentTypes = entry.Rule.AllowedContentTypes.Select(t => t.ToLowerInvariant()).ToArray(),
                    AllowedExtensions = entry.Rule.AllowedExtensions.Select(e => e.ToLowerInvariant()).ToArray(),
                    Description = UploadValidator.Describe(entry.Rule)
                }));
        }
    }
}

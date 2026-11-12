using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    /// <summary>
    /// Finalizes generated documents and exposes the permanent document history.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class DocumentController : ControllerBase
    {
        private static readonly JsonSerializerOptions DetailJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ApplicationDbContext _context;

        public DocumentController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// POST: api/documents/generate-id
        /// Finalizes an intern ID and stores a permanent GeneratedDocuments row.
        /// </summary>
        [HttpPost("generate-id")]
        public async Task<ActionResult<object>> GenerateId([FromBody] GenerateIdInput input)
        {
            if (input is null || input.InternId <= 0)
            {
                return BadRequest(new { message = "InternId is required." });
            }

            var documentType = NormalizeIdType(input.DocumentType);
            if (documentType is null)
            {
                return BadRequest(new { message = "DocumentType must be ID or Intern ID for this endpoint." });
            }

            var intern = await _context.Interns
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == input.InternId);

            if (intern is null)
            {
                return NotFound(new { message = $"Intern {input.InternId} was not found." });
            }

            var now = DateTime.Now;
            var issued = (input.IssuedDate ?? now).Date;

            var document = new GeneratedDocument
            {
                InternId = intern.Id,
                DocumentType = documentType,
                IssuedDate = issued,
                Details = BuildIdDetails(input, intern),
                SignatoryName = Clean(input.SignatoryName),
                FilePath = Clean(input.FilePath),
                CreatedAt = now
            };

            _context.GeneratedDocuments.Add(document);
            ActivityLogWriter.Add(
                _context,
                User,
                "POST",
                "/api/documents/generate-id",
                "Generated an intern ID",
                $"Saved an {documentType} record for {intern.FullName}.");

            await _context.SaveChangesAsync(HttpContext.RequestAborted);

            return Created(
                $"/api/documents/history/{intern.Id}",
                ToDto(document, intern.FullName));
        }

        /// <summary>
        /// GET: api/documents/history/{internId}
        /// Returns every finalized document that belongs to one intern profile.
        /// </summary>
        [HttpGet("history/{internId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> GetHistoryByIntern(int internId)
        {
            var intern = await _context.Interns
                .AsNoTracking()
                .Where(i => i.Id == internId)
                .Select(i => new { i.Id, i.FullName })
                .FirstOrDefaultAsync();

            if (intern is null)
            {
                return NotFound(new { message = $"Intern {internId} was not found." });
            }

            var documents = await _context.GeneratedDocuments
                .AsNoTracking()
                .Where(d => d.InternId == internId)
                .OrderByDescending(d => d.IssuedDate)
                .ThenByDescending(d => d.Id)
                .Select(d => new
                {
                    d.Id,
                    d.InternId,
                    InternName = intern.FullName,
                    d.DocumentType,
                    d.IssuedDate,
                    d.Details,
                    d.SignatoryName,
                    d.FilePath,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(documents);
        }

        /// <summary>
        /// GET: api/documents/history
        /// Global list of finalized documents, optionally filtered by intern or type.
        /// </summary>
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<object>>> GetHistory(
            [FromQuery] int? internId,
            [FromQuery] string? documentType,
            [FromQuery] int take = 200)
        {
            var query = _context.GeneratedDocuments
                .AsNoTracking()
                .AsQueryable();

            if (internId is int id)
            {
                query = query.Where(d => d.InternId == id);
            }

            if (!string.IsNullOrWhiteSpace(documentType))
            {
                var type = NormalizeDocumentType(documentType);
                query = query.Where(d => d.DocumentType == type);
            }

            take = Math.Clamp(take, 1, 1000);

            var documents = await query
                .OrderByDescending(d => d.IssuedDate)
                .ThenByDescending(d => d.Id)
                .Take(take)
                .Select(d => new
                {
                    d.Id,
                    d.InternId,
                    InternName = d.Intern != null ? d.Intern.FullName : null,
                    d.DocumentType,
                    d.IssuedDate,
                    d.Details,
                    d.SignatoryName,
                    d.FilePath,
                    d.CreatedAt
                })
                .ToListAsync();

            return Ok(documents);
        }

        /// <summary>
        /// GET: api/documents/status?documentType=Intern ID
        /// One row per intern that already has a finalized document of the
        /// requested type. The intern dropdowns use this to show the
        /// "Already Generated" badge without loading the whole history.
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<IEnumerable<object>>> GetStatus([FromQuery] string? documentType)
        {
            var query = _context.GeneratedDocuments
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(documentType))
            {
                var type = NormalizeDocumentType(documentType);
                query = query.Where(d => d.DocumentType == type);
            }

            var statuses = await query
                .GroupBy(d => d.InternId)
                .Select(g => new
                {
                    InternId = g.Key,
                    Count = g.Count(),
                    LastIssuedDate = g.Max(d => d.IssuedDate),
                    LastDocumentId = g.Max(d => d.Id)
                })
                .ToListAsync();

            return Ok(statuses);
        }

        private static object ToDto(GeneratedDocument document, string? internName) => new
        {
            document.Id,
            document.InternId,
            InternName = internName,
            document.DocumentType,
            document.IssuedDate,
            document.Details,
            document.SignatoryName,
            document.FilePath,
            document.CreatedAt
        };

        private static string BuildIdDetails(GenerateIdInput input, Intern intern)
        {
            if (input.Details is JsonElement details &&
                details.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
            {
                return details.ValueKind == JsonValueKind.String
                    ? (details.GetString() ?? string.Empty)
                    : details.GetRawText();
            }

            return JsonSerializer.Serialize(new
            {
                idNumber = Clean(input.IdNumber) ?? Clean(intern.ApplicantNo),
                office = Clean(input.Office) ?? Clean(intern.ApplicantOffice),
                school = Clean(intern.SchoolName) ?? Clean(intern.School),
                email = Clean(intern.Email),
                validUntil = input.ValidUntil?.Date
            }, DetailJsonOptions);
        }

        private static string? NormalizeIdType(string? value)
        {
            var type = Clean(value);
            if (type is null) return "Intern ID";

            if (type.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Intern ID", StringComparison.OrdinalIgnoreCase))
            {
                return "Intern ID";
            }

            return null;
        }

        private static string NormalizeDocumentType(string value)
        {
            var type = Clean(value) ?? string.Empty;
            if (type.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Intern ID", StringComparison.OrdinalIgnoreCase))
            {
                return "Intern ID";
            }

            if (type.Equals("COC", StringComparison.OrdinalIgnoreCase) ||
                type.Equals("Certificate of Completion", StringComparison.OrdinalIgnoreCase))
            {
                return "Certificate of Completion";
            }

            return type;
        }

        private static string? Clean(string? value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }

    /// <summary>
    /// Body for POST api/documents/generate-id. Details can be either a plain
    /// string or a JSON object; both are stored as text in GeneratedDocuments.
    /// </summary>
    public sealed class GenerateIdInput
    {
        public int InternId { get; set; }
        public string? DocumentType { get; set; }
        public DateTime? IssuedDate { get; set; }
        public JsonElement? Details { get; set; }
        public string? SignatoryName { get; set; }
        public string? FilePath { get; set; }
        public string? IdNumber { get; set; }
        public string? Office { get; set; }
        public string? SchoolName { get; set; }
        public DateTime? ValidUntil { get; set; }
    }
}

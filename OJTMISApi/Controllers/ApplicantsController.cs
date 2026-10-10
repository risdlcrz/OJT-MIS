using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    /// <summary>Body ng kahilingan para mag-preview ng start/end date.</summary>
    public class EstimateDatesRequest
    {
        public string? HireDate { get; set; }
        public int DurationDays { get; set; } = 1;
        public bool ExcludeFriday { get; set; }
    }

    /// <summary>Body ng kahilingan para mag-hire ng applicant.</summary>
    public class HireApplicantRequest
    {
        public int RequestId { get; set; }
        public string? HireDate { get; set; }
        public int DurationDays { get; set; }
        public bool ExcludeFriday { get; set; }
        public string? Remarks { get; set; }
        public string? Office { get; set; }
    }

    [Authorize(Roles = UserRoles.HRAdmin)]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ApplicantsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicantsController> _logger;

        public ApplicantsController(ApplicationDbContext context, ILogger<ApplicantsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>GET: api/applicants</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Applicant>>> GetAll()
        {
            var items = await _context.Applicants
                .AsNoTracking()
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>GET: api/applicants/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Applicant>> GetById(int id)
        {
            var applicant = await _context.Applicants.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (applicant is null) return NotFound(new { message = $"Applicant with id {id} was not found." });
            return Ok(applicant);
        }

        /// <summary>
        /// POST: api/applicants
        /// Gumagawa ng applicant no kung wala (A-2026-001).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Applicant>> Create([FromBody] Applicant applicant)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (await _context.Applicants.AnyAsync(a => a.ApplicantNo == applicant.ApplicantNo))
            {
                return Conflict(new { message = "An applicant with the same applicant number already exists." });
            }

            var entity = new Applicant();
            Map(applicant, entity);
            entity.ApplicantNo = string.IsNullOrWhiteSpace(applicant.ApplicantNo)
                ? await NextApplicantNoAsync()
                : applicant.ApplicantNo.Trim();
            entity.Id = 0;
            entity.Hired = false;
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;

            _context.Applicants.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
        }

        /// <summary>PUT: api/applicants/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Applicant>> Update(int id, [FromBody] Applicant applicant)
        {
            if (id != applicant.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var existing = await _context.Applicants.FirstOrDefaultAsync(a => a.Id == id);
            if (existing is null) return NotFound(new { message = $"Applicant with id {id} was not found." });

            if (!string.IsNullOrWhiteSpace(applicant.ApplicantNo)
                && applicant.ApplicantNo.Trim() != existing.ApplicantNo
                && await _context.Applicants.AnyAsync(a => a.ApplicantNo == applicant.ApplicantNo.Trim()))
            {
                return Conflict(new { message = "An applicant with the same applicant number already exists." });
            }

            Map(applicant, existing);
            if (!string.IsNullOrWhiteSpace(applicant.ApplicantNo))
            {
                existing.ApplicantNo = applicant.ApplicantNo.Trim();
            }
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        /// <summary>DELETE: api/applicants/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var applicant = await _context.Applicants.FindAsync(id);
            if (applicant is null) return NotFound(new { message = $"Applicant with id {id} was not found." });

            if (applicant.Hired)
            {
                return Conflict(new { message = "Cannot delete an applicant who has already been hired. Remove the intern record first." });
            }

            _context.Applicants.Remove(applicant);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// POST: api/applicants/estimate-dates
        /// Para m-preview ang estimated start at end date bago i-save.
        /// Ito ang tumatanggap ng flag na ExcludeFriday.
        /// </summary>
        [HttpPost("estimate-dates")]
        public ActionResult<object> EstimateDates([FromBody] EstimateDatesRequest request)
        {
            if (!DateTime.TryParse(request.HireDate, out var hireDate))
            {
                return BadRequest(new { message = "Please provide a valid hire date." });
            }

            if (request.DurationDays < 1)
            {
                return BadRequest(new { message = "Duration must be at least 1 day." });
            }

            var (start, end) = OjtDateCalculator.Compute(hireDate.Date, request.DurationDays, request.ExcludeFriday);

            return Ok(new
            {
                hireDate = hireDate.Date.ToString("yyyy-MM-dd"),
                durationDays = request.DurationDays,
                excludeFriday = request.ExcludeFriday,
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd"),
                totalCalendarDays = (int)(end - start).TotalDays + 1
            });
        }

        /// <summary>
        /// POST: api/applicants/{id}/hire
        /// Kinokopya ang buong profile ng applicant sa Intern, kinakalkula
        /// ang start/end date, kinakain ang isang slot sa request, at
        /// minarkahan ang applicant bilang Hired.
        /// </summary>
        [HttpPost("{id:int}/hire")]
        public async Task<ActionResult<object>> Hire(int id, [FromBody] HireApplicantRequest request)
        {
            var applicant = await _context.Applicants.FirstOrDefaultAsync(a => a.Id == id);
            if (applicant is null) return NotFound(new { message = $"Applicant with id {id} was not found." });

            if (applicant.Hired)
            {
                return Conflict(new { message = "This applicant has already been hired." });
            }

            if (!DateTime.TryParse(request.HireDate, out var hireDate))
            {
                return BadRequest(new { message = "Please provide a valid hire date." });
            }

            if (request.RequestId <= 0)
            {
                return BadRequest(new { message = "Please select an intern request." });
            }

            var duration = request.DurationDays < 1 ? 1 : request.DurationDays;

            var internRequest = await _context.InternRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId);
            if (internRequest is null)
            {
                return BadRequest(new { message = "The selected intern request does not exist." });
            }

            if (!internRequest.HasSlots)
            {
                return Conflict(new { message = $"Request from {internRequest.OfficeName} has no available slots left." });
            }

            var (start, end) = OjtDateCalculator.Compute(hireDate.Date, duration, request.ExcludeFriday);

            var fullName = BuildFullName(applicant);
            var now = DateTime.Now;

            // Look up the school from the Schools table to ensure exact name match for abbreviation lookup
            var school = await _context.Schools
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Name != null && 
                    s.Name.ToLower().Trim() == (applicant.SchoolName ?? string.Empty).ToLower().Trim());

            if (school is null)
            {
                return BadRequest(new { message = $"The school '{applicant.SchoolName}' does not exist in the schools list. Please add it first." });
            }

            var intern = new Intern
            {
                // Carry over ng buong profile ng applicant
                ApplicantId = applicant.Id,
                ApplicantNo = applicant.ApplicantNo,
                FirstName = applicant.FirstName,
                MiddleName = applicant.MiddleName,
                LastName = applicant.LastName,
                Suffix = applicant.Suffix,
                FullName = fullName,
                Email = applicant.Email,
                ContactNumber = applicant.ContactNumber,
                HouseAddress = applicant.HouseAddress,
                School = school.Name,
                SchoolName = school.Name,
                EducationLevel = applicant.EducationLevel,
                Program = applicant.Program,
                CoordName = applicant.CoordName,
                RequiredHours = applicant.RequiredHours,
                GuardianName = applicant.GuardianName,
                GuardianContact = applicant.GuardianContact,
                RequirementsCsv = applicant.RequirementsCsv,
                ApplicantOffice = string.IsNullOrWhiteSpace(request.Office) ? applicant.ApplicantOffice : request.Office!.Trim(),
                Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? applicant.Remarks : request.Remarks!.Trim(),

                RequestId = internRequest.Id,
                Status = "Hired",

                // OJT schedule
                HireDate = hireDate.Date,
                DurationDays = duration,
                ExcludeFriday = request.ExcludeFriday,
                StartDate = start,
                EndDate = end,

                CreatedAt = now,
                UpdatedAt = now
            };

            // Markahan ang applicant bilang Hired at itala ang lahat ng detalye
            applicant.Hired = true;
            applicant.Accepted = true;
            applicant.Rejected = false;
            applicant.RequestNo = internRequest.Id;
            applicant.RequestNoLabel = $"#{internRequest.Id} - {internRequest.OfficeName}";
            applicant.ApplicantOffice = intern.ApplicantOffice;
            applicant.Remarks = intern.Remarks;
            applicant.HireDate = hireDate.Date;
            applicant.DurationDays = duration;
            applicant.ExcludeFriday = request.ExcludeFriday;
            applicant.StartDate = start;
            applicant.EndDate = end;
            applicant.ActionDate = hireDate.Date;
            applicant.UpdatedAt = now;

            // Kainin ang isang slot
            internRequest.Filled += 1;
            internRequest.UpdatedAt = now;

            _context.Interns.Add(intern);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Intern profile created for {fullName}.",
                intern,
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd")
            });
        }

        /* ---------------- Helpers ---------------- */

        private static string BuildFullName(Applicant a)
        {
            var name = string.IsNullOrWhiteSpace(a.MiddleName)
                ? $"{a.LastName}, {a.FirstName}"
                : $"{a.LastName}, {a.FirstName} {a.MiddleName}";

            return string.IsNullOrWhiteSpace(a.Suffix) ? name : $"{name} {a.Suffix}";
        }

        private async Task<string> NextApplicantNoAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"A-{year}-";

            // Gamitin ang pinakamataas na numero (hindi ang count), kaya hindi
            // na-uulit ang numero kahit may naunang mga na-delete na applicant.
            var nos = await _context.Applicants
                .Where(a => a.ApplicantNo.StartsWith(prefix))
                .Select(a => a.ApplicantNo)
                .ToListAsync();

            var max = 0;
            foreach (var no in nos)
            {
                var tail = no.Substring(prefix.Length);
                if (int.TryParse(tail, out var n) && n > max) max = n;
            }

            var next = $"{prefix}{max + 1:D3}";

            // Siguraduhing hindi pa nagagamit (safety kung may butas sa numbering).
            while (await _context.Applicants.AnyAsync(a => a.ApplicantNo == next))
            {
                max++;
                next = $"{prefix}{max + 1:D3}";
            }

            return next;
        }

        /// <summary>Kopya ng mga field mula sa DTO papunta sa entity.</summary>
        private static void Map(Applicant src, Applicant dst)
        {
            dst.FirstName = src.FirstName?.Trim() ?? string.Empty;
            dst.MiddleName = src.MiddleName?.Trim() ?? string.Empty;
            dst.LastName = src.LastName?.Trim() ?? string.Empty;
            dst.Suffix = src.Suffix?.Trim() ?? string.Empty;
            dst.Email = src.Email?.Trim() ?? string.Empty;
            dst.ContactNumber = src.ContactNumber?.Trim() ?? string.Empty;
            dst.HouseAddress = src.HouseAddress?.Trim() ?? string.Empty;
            dst.SchoolName = src.SchoolName?.Trim() ?? string.Empty;
            dst.EducationLevel = src.EducationLevel?.Trim() ?? string.Empty;
            dst.Program = src.Program?.Trim() ?? string.Empty;
            dst.CoordName = src.CoordName?.Trim() ?? string.Empty;
            dst.RequiredHours = src.RequiredHours;
            dst.GuardianName = src.GuardianName?.Trim() ?? string.Empty;
            dst.GuardianContact = src.GuardianContact?.Trim() ?? string.Empty;

            // Requirements: pumili sa CSV (mula sa DTO) o sa array.
            var csv = src.RequirementsCsv;
            if (string.IsNullOrWhiteSpace(csv)) csv = null;
            if (csv is not null) dst.RequirementsCsv = csv;
            else dst.Requirements = src.Requirements ?? Array.Empty<bool>();

            dst.Accepted = src.Accepted;
            dst.Rejected = src.Rejected;
            dst.ApplicantOffice = src.ApplicantOffice?.Trim() ?? string.Empty;
            dst.Remarks = src.Remarks?.Trim() ?? string.Empty;
            dst.RequestNo = src.RequestNo;
            dst.RequestNoLabel = src.RequestNoLabel?.Trim() ?? string.Empty;
            dst.ActionDate = src.ActionDate;

            // Orientation: pumili sa flat fields o sa "orientation" object.
            if (src.OrientationDate.HasValue) dst.OrientationDate = src.OrientationDate;
            if (!string.IsNullOrWhiteSpace(src.OrientationTime)) dst.OrientationTime = src.OrientationTime.Trim();
            if (!string.IsNullOrWhiteSpace(src.OrientationOffice)) dst.OrientationOffice = src.OrientationOffice.Trim();
            dst.OrientationConfirmed = src.OrientationConfirmed;
        }
    }
}

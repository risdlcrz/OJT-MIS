using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    /// <summary>
    /// Renders the Certificate of Completion for an intern who has completed the
    /// required hours and emails the PDF to the intern's registered address.
    /// </summary>
    [ApiController]
    [Route("api/Certificate")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class CertificateController : ControllerBase
    {
        private const string ControllerPath = "/api/Certificate";
        private const double HourTolerance = 0.009;

        private readonly ApplicationDbContext _context;
        private readonly InternCompletionService _completion;
        private readonly IEmailService _email;
        private readonly ILogger<CertificateController> _logger;

        /// <summary>
        /// The COA seal is embedded in this assembly, so generating a certificate
        /// never depends on the working directory. Decoded once per process.
        /// </summary>
        private static readonly Lazy<byte[]?> Seal = new(LoadSeal, isThreadSafe: true);

        public CertificateController(
            ApplicationDbContext context,
            InternCompletionService completion,
            IEmailService email,
            ILogger<CertificateController> logger)
        {
            _context = context;
            _completion = completion;
            _email = email;
            _logger = logger;
        }

        /// <summary>
        /// POST: api/Certificate/GenerateAndSend/{internId}
        /// Renders the Certificate of Completion and emails it to the intern.
        /// The optional body mirrors the choices made on the COC page; when it
        /// is omitted the first signatory in the directory is used.
        /// </summary>
        [HttpPost("GenerateAndSend/{internId:int}")]
        public async Task<ActionResult<object>> GenerateAndSend(
            int internId,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CertificateSendInput? input)
        {
            var intern = await _context.Interns
                .FirstOrDefaultAsync(i => i.Id == internId);

            if (intern is null)
            {
                return NotFound(new { message = $"Intern {internId} was not found." });
            }

            var required = intern.RequiredHours;
            var rendered = Math.Round(
                await _completion.RenderedHoursAsync(internId, HttpContext.RequestAborted), 2);

            // Same rule as GET api/HR/Certificates/Eligible: the intern must have
            // a configured requirement and must have rendered at least that many
            // hours from attendance logs that already have a time out.
            if (required <= 0 || rendered + HourTolerance < required)
            {
                return BadRequest(new
                {
                    message = required <= 0
                        ? "This intern has no required hours configured, so a certificate cannot be generated."
                        : $"This intern has rendered {CertificatePdf.FormatHours(rendered)} of " +
                          $"{CertificatePdf.FormatHours(required)} required hours, so a certificate cannot be generated yet."
                });
            }

            var recipient = intern.Email?.Trim() ?? string.Empty;
            if (recipient.Length == 0)
            {
                return BadRequest(new { message = "This intern has no registered email address to send the certificate to." });
            }

            var name = CertificateName(intern);
            var signatory = await ResolveSignatoryAsync(input);
            var issued = input?.DateIssued?.Date ?? DateTime.Today;

            var model = new CertificatePdfModel
            {
                Honorific = (input?.Honorific ?? string.Empty).Trim(),
                FullName = name,
                School = FirstNonEmpty(intern.SchoolName, intern.School),
                Office = FirstNonEmpty(input?.Office, intern.ApplicantOffice),
                HoursRendered = rendered,
                RequiredHours = required,
                DateStarted = intern.StartDate,
                DateCompleted = intern.EndDate,
                DateIssued = issued,
                SignatoryName = signatory.Name,
                SignatoryPosition = signatory.Position,
                SignatoryOffice = signatory.Department,
                A4Paper = string.Equals(input?.Paper?.Trim(), "a4", StringComparison.OrdinalIgnoreCase)
            };

            byte[] pdf;
            try
            {
                pdf = CertificatePdf.Render(model, Seal.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not render the Certificate of Completion for intern {InternId}.", internId);
                return Problem(
                    title: "Certificate rendering failed",
                    detail: "The certificate could not be rendered. Please try again.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var fileName = $"Certificate-of-Completion-{Slug(name)}.pdf";
            var subject = "Your Certificate of Completion - Commission on Audit";
            var html = EmailTemplates.CertificateOfCompletion(name, CertificatePdf.FormatHours(rendered));

            try
            {
                await _email.SendEmailAsync(recipient, subject, html, pdf, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not email the Certificate of Completion for intern {InternId} to {Email}.",
                    internId,
                    recipient);
                return Problem(
                    title: "Email delivery failed",
                    detail: "The certificate was generated but could not be emailed. Please check the mail configuration and try again.",
                    statusCode: StatusCodes.Status502BadGateway);
            }

            try
            {
                ActivityLogWriter.Add(
                    _context,
                    User,
                    "POST",
                    $"{ControllerPath}/GenerateAndSend/{internId}",
                    "Emailed a Certificate of Completion",
                    $"Sent the Certificate of Completion for {name} to {recipient}.");
                await _context.SaveChangesAsync(HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                // The email already went out; a failed audit write must not make
                // HR retry and send the certificate twice.
                _logger.LogWarning(ex, "Certificate for intern {InternId} was emailed but the activity log was not written.", internId);
            }

            return Ok(new
            {
                message = $"Certificate of Completion sent to {recipient}.",
                sentTo = recipient,
                fileName
            });
        }

        /// <summary>
        /// Mirrors the name shown on the COC preview: first name, middle initial,
        /// last name, and suffix, falling back to the stored full name.
        /// </summary>
        private static string CertificateName(Intern intern)
        {
            var first = (intern.FirstName ?? string.Empty).Trim();
            var middle = (intern.MiddleName ?? string.Empty).Trim();
            var last = (intern.LastName ?? string.Empty).Trim();
            var suffix = (intern.Suffix ?? string.Empty).Trim();

            var name = first.Length > 0 && last.Length > 0
                ? $"{first}{(middle.Length > 0 ? $" {char.ToUpperInvariant(middle[0])}." : string.Empty)} {last}{(suffix.Length > 0 ? $" {suffix}" : string.Empty)}"
                : (intern.FullName ?? string.Empty).Trim();

            return name.ToUpperInvariant();
        }

        /// <summary>
        /// Uses the signatory chosen on the COC page, then any explicit fields in
        /// the request, and finally the first signatory in the directory (which is
        /// what the page selects by default).
        /// </summary>
        private async Task<Signatory> ResolveSignatoryAsync(CertificateSendInput? input)
        {
            Signatory? signatory = null;

            if (input?.SignatoryId is int id)
            {
                signatory = await _context.Signatories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);
            }

            signatory ??= await _context.Signatories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .FirstOrDefaultAsync()
                ?? new Signatory();

            // Explicit fields from the page win over the stored record.
            return new Signatory
            {
                Id = signatory.Id,
                Name = FirstNonEmpty(input?.SignatoryName, signatory.Name).ToUpperInvariant(),
                Position = FirstNonEmpty(input?.SignatoryPosition, signatory.Position),
                Department = FirstNonEmpty(input?.SignatoryOffice, signatory.Department)
            };
        }

        private static string FirstNonEmpty(params string?[] values) =>
            values.Select(value => value?.Trim() ?? string.Empty)
                .FirstOrDefault(value => value.Length > 0) ?? string.Empty;

        private static string Slug(string value)
        {
            // "Muñoz" must become "MUNOZ", not "MU-OZ": drop the diacritics
            // before turning anything else into a dash.
            var decomposed = value.Normalize(NormalizationForm.FormD);
            var ascii = new string(decomposed
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                .ToArray());

            var slug = Regex.Replace(ascii, "[^A-Za-z0-9]+", "-").Trim('-');
            return slug.Length == 0 ? "Intern" : slug;
        }

        private static byte[]? LoadSeal()
        {
            try
            {
                var assembly = typeof(CertificateController).Assembly;
                var resource = assembly.GetManifestResourceNames()
                    .FirstOrDefault(name => name.EndsWith("coa-logo.png", StringComparison.OrdinalIgnoreCase));

                if (resource is null) return null;

                using var stream = assembly.GetManifestResourceStream(resource);
                if (stream is null) return null;

                using var buffer = new MemoryStream();
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
            catch
            {
                // A missing seal must never block the certificate.
                return null;
            }
        }
    }

    /// <summary>
    /// Optional body for the COC endpoint. Every field is optional because the
    /// certificate can also be generated from the stored intern and signatory
    /// records alone (for example from an integration or a notification link).
    /// </summary>
    public sealed class CertificateSendInput
    {
        public string? Honorific { get; set; }

        /// <summary>Office or place of training, when HR edited it on the COC page.</summary>
        public string? Office { get; set; }

        public int? SignatoryId { get; set; }

        public string? SignatoryName { get; set; }

        public string? SignatoryPosition { get; set; }

        public string? SignatoryOffice { get; set; }

        public DateTime? DateIssued { get; set; }

        /// <summary>"a4" for A4 landscape; anything else renders Letter landscape.</summary>
        public string? Paper { get; set; }
    }
}

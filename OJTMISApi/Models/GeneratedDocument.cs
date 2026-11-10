using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Permanent record of a document issued to an intern (an ID card or a
    /// Certificate of Completion). Saved once the finalized details are
    /// generated and submitted, so the exact contents handed to the intern
    /// can still be reviewed from the intern's profile later on.
    /// </summary>
    public class GeneratedDocument
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Intern the document belongs to. Null when an ID card was printed without linking an intern.</summary>
        public int? InternId { get; set; }

        [Required, StringLength(200)]
        public string InternName { get; set; } = string.Empty;

        /// <summary>"ID" or "Certificate of Completion".</summary>
        [Required, StringLength(80)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>Date written on the document (as issued).</summary>
        public DateTime? DateIssued { get; set; }

        [StringLength(200)]
        public string Office { get; set; } = string.Empty;

        [StringLength(200)]
        public string School { get; set; } = string.Empty;

        /* ---- Intern ID card details ---- */

        [StringLength(50)]
        public string IdNumber { get; set; } = string.Empty;

        public DateTime? ValidUntil { get; set; }

        [StringLength(300)]
        public string SchoolAddress { get; set; } = string.Empty;

        [StringLength(200)]
        public string EmergencyContactName { get; set; } = string.Empty;

        [StringLength(50)]
        public string EmergencyContactNumber { get; set; } = string.Empty;

        [StringLength(300)]
        public string EmergencyContactAddress { get; set; } = string.Empty;

        /* ---- Certificate of Completion details ---- */

        [StringLength(20)]
        public string Honorific { get; set; } = string.Empty;

        /// <summary>Attendance hours the intern had rendered when the certificate was issued.</summary>
        public double? HoursRendered { get; set; }

        public int? RequiredHours { get; set; }

        public DateTime? DateStarted { get; set; }

        public DateTime? DateCompleted { get; set; }

        /// <summary>Email address the certificate PDF was sent to (certificate only).</summary>
        [StringLength(200)]
        public string RecipientEmail { get; set; } = string.Empty;

        /* ---- Common issuance details ---- */

        [StringLength(200)]
        public string SignatoryName { get; set; } = string.Empty;

        [StringLength(150)]
        public string SignatoryPosition { get; set; } = string.Empty;

        [StringLength(200)]
        public string SignatoryOffice { get; set; } = string.Empty;

        /// <summary>HR user who generated the document.</summary>
        [StringLength(200)]
        public string GeneratedBy { get; set; } = string.Empty;

        /// <summary>When the record was saved (issuance time).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

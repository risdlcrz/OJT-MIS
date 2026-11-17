using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>Represents a Interns record for the Intern module.</summary>
    public class Intern
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(200, ErrorMessage = "Full Name cannot exceed 200 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "School is required.")]
        [StringLength(200, ErrorMessage = "School cannot exceed 200 characters.")]
        public string School { get; set; } = string.Empty;

        /// <summary>
        /// Id of the intern request this hire came from.
        /// Needed to take a slot from that request.
        /// </summary>
        public int? RequestId { get; set; }

        /// <summary>
        /// "Hired" or "Applicant". "Hired" records are treated as
        /// interns and shown in the Intern List.
        /// </summary>
        [Required, StringLength(20)]
        public string Status { get; set; } = "Applicant";

        /* ============================================================
         * Profile copied from the Applicant at the time of hiring.
         * Every field here is a copy of the applicant's data.
         * ============================================================ */

        /// <summary>Id of the Applicant this intern came from.</summary>
        public int? ApplicantId { get; set; }

        [StringLength(20)]
        public string ApplicantNo { get; set; } = string.Empty;

        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string MiddleName { get; set; } = string.Empty;

        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(10)]
        public string Suffix { get; set; } = string.Empty;

        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string ContactNumber { get; set; } = string.Empty;

        [StringLength(300)]
        public string HouseAddress { get; set; } = string.Empty;

        [StringLength(200)]
        public string SchoolName { get; set; } = string.Empty;

        [StringLength(100)]
        public string EducationLevel { get; set; } = string.Empty;

        [StringLength(200)]
        public string Program { get; set; } = string.Empty;

        [StringLength(200)]
        public string CoordName { get; set; } = string.Empty;

        public int RequiredHours { get; set; }

        /// <summary>Sum of completed attendance punches. Updated on punch and adjustment approval.</summary>
        public double HoursRendered { get; set; }

        /// <summary>
        /// Opaque attendance code printed on the intern's current ID
        /// ("OJT2." + 256 random bits). It carries no intern data, is unique
        /// across all interns, and is replaced each time HR generates a new
        /// ID, which cancels every previously printed card.
        /// </summary>
        [StringLength(64)]
        public string? AttendanceQrToken { get; set; }

        /// <summary>When the current <see cref="AttendanceQrToken"/> was issued.</summary>
        public DateTime? AttendanceQrIssuedAt { get; set; }

        [StringLength(200)]
        public string GuardianName { get; set; } = string.Empty;

        [StringLength(50)]
        public string GuardianContact { get; set; } = string.Empty;

        [StringLength(50)]
        public string RequirementsCsv { get; set; } = "0,0,0,0,0,0";

        [StringLength(200)]
        public string ApplicantOffice { get; set; } = string.Empty;

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;

        /* ---- OJT schedule ---- */
        public DateTime? HireDate { get; set; }
        public int DurationDays { get; set; }
        public bool ExcludeFriday { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Set once the intern reaches the required hours and HR has been
        /// notified, so the completion notification is only sent one time.
        /// </summary>
        public DateTime? CompletionNotifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        public DateTime UpdatedAt { get; set; } = AppClock.Now;
    }
}

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
        /// Id ng intern request na pinanggalingan ng hire na ito.
        /// Kailangan ito upang mabawasan ang slot ng request.
        /// </summary>
        public int? RequestId { get; set; }

        /// <summary>
        /// "Hired" o "Applicant". Ang mga naka-"Hired" ay awtomatikong
        /// itinuturing na intern at ipinapakita sa Intern List.
        /// </summary>
        [Required, StringLength(20)]
        public string Status { get; set; } = "Applicant";

        /* ============================================================
         * Profile na kinopya mula sa Applicant sa oras ng pag-hire.
         * Lahat ng field dito ay duma-doble sa data ng aplikante.
         * ============================================================ */

        /// <summary>Id ng Applicant na pinagmulanan ng intern.</summary>
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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Isang applicant ng OJT. Kapag na-hire, ang buong profile na ito
    /// ay kinopya sa <see cref="Intern"/> (tingnan ang ApplicantsController.Hire).
    /// </summary>
    public class Applicant
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Auto-generated (A-2026-001) kapag iniwanang blangko sa POST.
        /// </summary>
        [StringLength(20)]
        public string ApplicantNo { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string MiddleName { get; set; } = string.Empty;

        [Required, StringLength(100)]
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

        [Range(0, 10000)]
        public int RequiredHours { get; set; }

        [StringLength(200)]
        public string GuardianName { get; set; } = string.Empty;

        [StringLength(50)]
        public string GuardianContact { get; set; } = string.Empty;

        /// <summary>
        /// Anim na requirements, nakastore bilang "1,0,1,0,0,0".
        /// Basahin gamit ang <see cref="Requirements"/> para sa array.
        /// </summary>
        [StringLength(50)]
        public string RequirementsCsv { get; set; } = "0,0,0,0,0,0";

        /// <summary>Array ng 6 booleans para sa mga requirements.</summary>
        [NotMapped]
        public bool[] Requirements
        {
            get => RequirementsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(v => v.Trim() == "1")
                                      .Concat(Enumerable.Repeat(false, 6))
                                      .Take(6)
                                      .ToArray();
            set => RequirementsCsv = string.Join(",", (value ?? Array.Empty<bool>())
                                                        .Concat(Enumerable.Repeat(false, 6))
                                                        .Take(6)
                                                        .Select(b => b ? "1" : "0"));
        }

        public bool Accepted { get; set; }
        public bool Rejected { get; set; }
        public bool Hired { get; set; }

        [StringLength(200)]
        public string ApplicantOffice { get; set; } = string.Empty;

        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>Id ng intern request na ginamit sa pag-hire.</summary>
        public int? RequestNo { get; set; }

        [StringLength(200)]
        public string RequestNoLabel { get; set; } = string.Empty;

        public DateTime? ActionDate { get; set; }

        /* ---- Orientation (Schedule Orientation tab) ---- */
        public DateTime? OrientationDate { get; set; }
        [StringLength(20)]
        public string OrientationTime { get; set; } = string.Empty;
        [StringLength(200)]
        public string OrientationOffice { get; set; } = string.Empty;
        public bool OrientationConfirmed { get; set; }

        /// <summary>
        /// Object na "orientation" na ginagamit ng SPA
        /// (date / time / office / confirmed). Kapag nakatakda ng SPA,
        /// awtomatikong naii-update ang mga flat column sa itaas.
        /// </summary>
        [NotMapped]
        public OrientationInfo Orientation
        {
            get => new OrientationInfo
            {
                Date = OrientationDate?.ToString("yyyy-MM-dd"),
                Time = OrientationTime,
                Office = OrientationOffice,
                Confirmed = OrientationConfirmed
            };
            set
            {
                if (value is null) return;
                if (DateTime.TryParse(value.Date, out var d)) OrientationDate = d;
                if (!string.IsNullOrWhiteSpace(value.Time)) OrientationTime = value.Time.Trim();
                if (!string.IsNullOrWhiteSpace(value.Office)) OrientationOffice = value.Office.Trim();
                OrientationConfirmed = value.Confirmed;
            }
        }

        /* ---- Hiring / OJT dates ---- */
        public DateTime? HireDate { get; set; }

        /// <summary> Bilang ng araw ng OJT na ibibilang sa range.</summary>
        [Range(0, 3660)]
        public int DurationDays { get; set; }

        /// <summary>Kung true, hindi binibilang ang tuwing BIYERNETES.</summary>
        public bool ExcludeFriday { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>Detalye ng orientation para sa Schedule Orientation tab.</summary>
    public class OrientationInfo
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? Office { get; set; }
        public bool Confirmed { get; set; }
    }
}

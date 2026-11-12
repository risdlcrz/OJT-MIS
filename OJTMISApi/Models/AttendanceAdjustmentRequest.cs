using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Punch column an adjustment may change.
    /// </summary>
    public static class AttendanceAdjustmentField
    {
        public const string MorningIn = "AM_In";
        public const string MorningOut = "AM_Out";
        public const string AfternoonIn = "PM_In";
        public const string AfternoonOut = "PM_Out";

        public static readonly string[] All = { MorningIn, MorningOut, AfternoonIn, AfternoonOut };

        public static bool IsTimeIn(string field) =>
            field is MorningIn or AfternoonIn;

        public static bool IsMorning(string field) =>
            field is MorningIn or MorningOut;

        public static string Label(string field) => field switch
        {
            MorningIn => "AM Time In",
            MorningOut => "AM Time Out",
            AfternoonIn => "PM Time In",
            AfternoonOut => "PM Time Out",
            _ => field
        };

        public static string? Normalize(string? value)
        {
            var text = (value ?? string.Empty).Trim();
            foreach (var field in All)
            {
                if (text.Equals(field, StringComparison.OrdinalIgnoreCase) ||
                    text.Equals(Label(field), StringComparison.OrdinalIgnoreCase))
                {
                    return field;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Review states for an attendance adjustment request.
    /// </summary>
    public static class AttendanceAdjustmentStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static readonly string[] All = { Pending, Approved, Rejected };
    }

    /// <summary>
    /// Intern request to correct one punch (AM/PM time in or time out).
    /// HR reviews the request and either approves or rejects it.
    /// </summary>
    public class AttendanceAdjustmentRequest
    {
        [Key]
        public int Id { get; set; }

        public int AttendanceLogId { get; set; }

        [ForeignKey(nameof(AttendanceLogId))]
        public DailyTimeRecord? AttendanceLog { get; set; }

        public int InternId { get; set; }

        [ForeignKey(nameof(InternId))]
        public Intern? Intern { get; set; }

        /// <summary>Corrected punch time the intern is requesting.</summary>
        [Required(ErrorMessage = "Requested time is required.")]
        public DateTime RequestedTime { get; set; }

        /// <summary>
        /// <see cref="AttendanceAdjustmentField.MorningIn"/>,
        /// <see cref="AttendanceAdjustmentField.MorningOut"/>,
        /// <see cref="AttendanceAdjustmentField.AfternoonIn"/>, or
        /// <see cref="AttendanceAdjustmentField.AfternoonOut"/>.
        /// </summary>
        [Required, StringLength(20)]
        public string TargetField { get; set; } = AttendanceAdjustmentField.MorningOut;

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Status { get; set; } = AttendanceAdjustmentStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? ReviewedBy { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public ApplicationUser? Reviewer { get; set; }

        /// <summary>
        /// Designated DTR approving officer (signatory directory entry) that
        /// reviewed the request. Kept alongside <see cref="ReviewedBy"/> so the
        /// approval remains attributable even if the login account is removed.
        /// </summary>
        public int? ApproverSignatoryId { get; set; }

        [ForeignKey(nameof(ApproverSignatoryId))]
        public Signatory? ApproverSignatory { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}

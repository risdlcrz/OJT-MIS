using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Allowable targets for an attendance adjustment. Mirrors the
    /// AM/PM time-out shifts used by <see cref="AttendanceShift"/>.
    /// </summary>
    public static class AttendanceAdjustmentShift
    {
        public const string MorningOut = "AM_Out";
        public const string AfternoonOut = "PM_Out";

        public static readonly string[] All = { MorningOut, AfternoonOut };
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
    /// Request submitted by an intern to correct a missing or incorrect
    /// time-out on one of their attendance records. HR/Supervisor reviews
    /// the request and either approves or rejects it.
    /// </summary>
    public class AttendanceAdjustmentRequest
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Attendance log (Daily Time Record) being corrected.
        /// </summary>
        public int AttendanceLogId { get; set; }

        [ForeignKey(nameof(AttendanceLogId))]
        public DailyTimeRecord? AttendanceLog { get; set; }

        /// <summary>Intern who filed the request.</summary>
        public int InternId { get; set; }

        [ForeignKey(nameof(InternId))]
        public Intern? Intern { get; set; }

        /// <summary>Corrected time-out the intern is requesting.</summary>
        [Required(ErrorMessage = "Requested time-out is required.")]
        public DateTime RequestedTimeOut { get; set; }

        /// <summary>
        /// Shift the request targets: <see cref="AttendanceAdjustmentShift.MorningOut"/>
        /// or <see cref="AttendanceAdjustmentShift.AfternoonOut"/>.
        /// </summary>
        [Required, StringLength(20)]
        public string TargetShift { get; set; } = AttendanceAdjustmentShift.MorningOut;

        [Required(ErrorMessage = "Reason is required.")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="AttendanceAdjustmentStatus.Pending"/>,
        /// <see cref="AttendanceAdjustmentStatus.Approved"/>, or
        /// <see cref="AttendanceAdjustmentStatus.Rejected"/>.
        /// </summary>
        [Required, StringLength(20)]
        public string Status { get; set; } = AttendanceAdjustmentStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        /// <summary>Identity user id of the reviewer; null while pending.</summary>
        [StringLength(450)]
        public string? ReviewedBy { get; set; }

        [ForeignKey(nameof(ReviewedBy))]
        public ApplicationUser? Reviewer { get; set; }

        public DateTime? ReviewedAt { get; set; }
    }
}

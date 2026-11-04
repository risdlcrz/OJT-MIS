using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>Represents a Daily Time Record (DTR) entry for the DTR module.</summary>
    public class DailyTimeRecord
    {
        [Key]
        public int Id { get; set; }

        public int? InternId { get; set; }

        [StringLength(200)]
        public string? InternName { get; set; }

        [StringLength(200)]
        public string? InternEmail { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        /// <summary>Morning (before noon) or Afternoon (noon and later).</summary>
        [Required, StringLength(20)]
        public string ShiftType { get; set; } = AttendanceShift.Morning;

        [Required(ErrorMessage = "Time In is required.")]
        public DateTime TimeIn { get; set; }

        public DateTime? TimeOut { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = AttendanceShift.OnTime;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

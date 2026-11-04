using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>Represents a Daily Time Record (DTR) entry for the DTR module.</summary>
    public class DailyTimeRecord
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Time In is required.")]
        public DateTime TimeIn { get; set; }

        public DateTime? TimeOut { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = "Present";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

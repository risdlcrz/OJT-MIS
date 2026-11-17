using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// An intern request (open slots) that HR can hire interns against.
    /// <see cref="Count"/> is the total number of slots and
    /// <see cref="Filled"/> is how many were already hired.
    /// </summary>
    public class InternRequest
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string OfficeCode { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string OfficeName { get; set; } = string.Empty;

        [Range(1, 9999, ErrorMessage = "Slots must be at least 1.")]
        public int Count { get; set; } = 1;

        public int Filled { get; set; }

        [StringLength(500)]
        public string Skills { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        public DateTime UpdatedAt { get; set; } = AppClock.Now;

        /// <summary>Optional signatory assigned to this request.</summary>
        public int? SignatoryId { get; set; }

        [ForeignKey(nameof(SignatoryId))]
        public Signatory? Signatory { get; set; }

        /// <summary>Slots still open. Zero means none are left.</summary>
        [NotMapped]
        public int Remaining => Math.Max(0, Count - Filled);

        /// <summary>True when the request is Open and still has slots.</summary>
        [NotMapped]
        public bool HasSlots => Status.Equals("Open", StringComparison.OrdinalIgnoreCase) && Remaining > 0;
    }
}

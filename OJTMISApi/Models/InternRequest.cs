using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Isang intern request (bakante) na maaaring kunin ng HR upang
    /// mag-hire ng intern. Ang <see cref="Count"/> ay kabuuang slot,
    /// at <see cref="Filled"/> ay dami nang na-hire.
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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>Pera pang natitirang slot. Zero = walang bakante na.</summary>
        [NotMapped]
        public int Remaining => Math.Max(0, Count - Filled);

        /// <summary>True kapag may pang paano pang slot.</summary>
        [NotMapped]
        public bool HasSlots => Status.Equals("Open", StringComparison.OrdinalIgnoreCase) && Remaining > 0;
    }
}

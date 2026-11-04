using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    public class DocumentRequest
    {
        [Key]
        public int Id { get; set; }

        public int InternId { get; set; }

        [Required, StringLength(200)]
        public string InternName { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string InternEmail { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string DocumentType { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

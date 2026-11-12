using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>Represents a generated document stored for an intern profile.</summary>
    public class GeneratedDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InternId { get; set; }

        [Required, StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        public DateTime IssuedDate { get; set; }

        /// <summary>Additional document details (plain text or JSON, e.g. hours, email).</summary>
        [StringLength(4000)]
        public string? Details { get; set; }

        [StringLength(200)]
        public string? SignatoryName { get; set; }

        /// <summary>File path or blob/reference to the stored document content.</summary>
        [StringLength(1000)]
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Intern? Intern { get; set; }
    }
}

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

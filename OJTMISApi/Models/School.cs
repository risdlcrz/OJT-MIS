using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Represents a school that interns can be assigned to.
    /// </summary>
    public class School
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "School name is required.")]
        [StringLength(200, ErrorMessage = "School name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

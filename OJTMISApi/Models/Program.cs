using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>Represents a program/strand record for the Program module.</summary>
    public class OJTProgram
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Program Code is required.")]
        [StringLength(50, ErrorMessage = "Program Code cannot exceed 50 characters.")]
        public string ProgramCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Program Name is required.")]
        [StringLength(200, ErrorMessage = "Program Name cannot exceed 200 characters.")]
        public string ProgramName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}


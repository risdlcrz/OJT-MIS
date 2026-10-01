using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>Represents a Signatories record for the Signatory module.</summary>
    public class Signatory
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(150, ErrorMessage = "Position cannot exceed 150 characters.")]
        public string Position { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Department cannot exceed 200 characters.")]
        public string Department { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

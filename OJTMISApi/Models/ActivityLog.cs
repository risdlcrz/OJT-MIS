using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Isang audit entry para sa bawat aksyon na ginawa ng isang user.
    /// Ang Category ay HR, Supervisor, o Intern batay sa role noong ginawa
    /// ang aksyon.
    /// </summary>
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required, StringLength(200)]
        public string UserName { get; set; } = string.Empty;

        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        /// <summary>HR, Supervisor, o Intern.</summary>
        [Required, StringLength(20)]
        public string Category { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string Method { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Path { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Action { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Details { get; set; }

        /// <summary>True kapag matagumpay na naisagawa ang aksyon.</summary>
        public bool IsVerified { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

using System.ComponentModel.DataAnnotations;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Isang notification na ipinapakita sa bell menu ng top navbar.
    /// Awtomatikong ginagawa ng <see cref="Services.NotificationInterceptor"/>
    /// tuwing may CRUD na nangyari sa monitored modules.
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Optional app link opened when the notification is clicked,
        /// for example <c>/coc-generate.html?internId=12</c>.
        /// </summary>
        [StringLength(300)]
        public string? Link { get; set; }

        /// <summary>Intern the notification is about, when applicable.</summary>
        public int? InternId { get; set; }

        /// <summary>
        /// Short machine-readable kind (for example <c>InternCompleted</c>)
        /// so targeted notifications can be de-duplicated.
        /// </summary>
        [StringLength(50)]
        public string? Type { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

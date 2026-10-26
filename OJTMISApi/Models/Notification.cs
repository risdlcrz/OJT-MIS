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

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

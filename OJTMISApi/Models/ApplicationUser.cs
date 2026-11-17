using Microsoft.AspNetCore.Identity;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Application user. Built on IdentityUser, which provides password
    /// hashing, roles, and the security stamp.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Office { get; set; }

        /// <summary>Profile picture as a data URL (already resized to a 256x256 JPEG).</summary>
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;

        /// <summary>
        /// Random password given to a new intern account.
        /// Cleared once the user changes their password.
        /// </summary>
        public string? TemporaryPassword { get; set; }

        /// <summary>
        /// HR side of the two-step account verification. Accounts HR creates
        /// (hiring, User Management, signatories) are approved at creation;
        /// self-registered accounts wait for HR to approve them. Sign-in needs
        /// both this flag and <c>EmailConfirmed</c>
        /// (the user's side, set from the emailed activation link).
        /// </summary>
        public bool IsApproved { get; set; }

        /// <summary>Name shown in the top bar (First + Last).</summary>
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// The roles in the system.
    /// </summary>
    public static class UserRoles
    {
        public const string HRAdmin = "HRAdmin";
        public const string Supervisor = "Supervisor";
        public const string Intern = "Intern";

        public static readonly string[] All = { HRAdmin, Supervisor, Intern };
    }
}

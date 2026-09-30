using Microsoft.AspNetCore.Identity;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Application user. Inherit sa IdentityUser para may built-in na
    /// password hashing, roles, at security stamp.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Office { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Pangalan na ipinapakita sa navbar (First + Last).</summary>
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Mga available na role sa system.
    /// </summary>
    public static class UserRoles
    {
        public const string HRAdmin = "HRAdmin";
        public const string Supervisor = "Supervisor";
        public const string Intern = "Intern";

        public static readonly string[] All = { HRAdmin, Supervisor, Intern };
    }
}

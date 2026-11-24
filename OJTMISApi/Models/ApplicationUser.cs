using System.ComponentModel.DataAnnotations;
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

        // ---- Account lock (see AccountLockService) ----------------------
        // Enforcement uses Identity's own LockoutEnd, so SignInManager and
        // the per-request token check agree on who is locked. These fields
        // record who locked it, when and why, for HR and the audit trail.

        /// <summary>Office-local time the account was locked; null when not locked.</summary>
        public DateTime? LockedAt { get; set; }

        /// <summary>HR admin who locked it; null when the system locked it after failed sign-ins.</summary>
        [StringLength(450)]
        public string? LockedByUserId { get; set; }

        /// <summary>Display name of whoever locked it (an HR admin, or "System").</summary>
        [StringLength(200)]
        public string? LockedByName { get; set; }

        /// <summary>Why the account was locked. Shown to HR and to the locked user's open sessions.</summary>
        [StringLength(500)]
        public string? LockReason { get; set; }

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

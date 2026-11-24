using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Locks and unlocks accounts.
    ///
    /// Sign-in failures are counted by ASP.NET Identity
    /// (<c>AccessFailedCount</c>, <see cref="MaxFailedSignInAttempts"/> in
    /// Program.cs). When the 5th failure trips Identity's lockout, the login
    /// endpoint calls <see cref="LockAfterFailedSignInsAsync"/>, which turns
    /// it into a lock that only an HR admin can lift, and alerts every HR
    /// admin. HR can also lock an account by hand.
    ///
    /// A lock is enforced through Identity's <c>LockoutEnd</c> (set to
    /// <see cref="DateTimeOffset.MaxValue"/>), so the sign-in check and the
    /// per-request token check (<see cref="JwtTokenService.ValidateAsync"/>)
    /// both refuse it. Locking also rotates the security stamp, so sessions
    /// opened before the lock stay dead after an unlock.
    /// </summary>
    public class AccountLockService
    {
        public const int MaxFailedSignInAttempts = 5;

        /// <summary>Response code the Vue client uses to sign the user out and explain why.</summary>
        public const string LockedCode = "account_locked";

        public const string SystemActor = "System";

        public const string FailedSignInReason = "Too many failed sign-in attempts (5).";

        /// <summary>Shown on the sign-in page. Gives no reason, so it reveals nothing to someone guessing passwords.</summary>
        public const string SignInLockedMessage =
            "This account is locked. Please contact an administrator to restore access.";

        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountLockService> _logger;

        public AccountLockService(
            UserManager<ApplicationUser> users,
            ApplicationDbContext context,
            ILogger<AccountLockService> logger)
        {
            _users = users;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// True while Identity's lockout is active. Covers both HR/system locks
        /// and any short lockout left over from before this feature existed.
        /// </summary>
        public static bool IsLocked(ApplicationUser user) =>
            user.LockoutEnd is { } end && end > DateTimeOffset.UtcNow;

        /// <summary>
        /// Message for a signed-in session (or an HR action) refused because the
        /// account is locked. Includes the reason: the session holder already
        /// proved who they are.
        /// </summary>
        public static string SessionLockedMessage(ApplicationUser user)
        {
            var reason = string.IsNullOrWhiteSpace(user.LockReason) ? null : user.LockReason.Trim();
            return reason is null
                ? "Your account has been locked. Please contact an administrator to restore access."
                : $"Your account has been locked. Reason: {reason} Please contact an administrator to restore access.";
        }

        /// <summary>
        /// Called when a sign-in attempt trips Identity's lockout. Makes the
        /// lock permanent until HR unlocks it, and alerts every HR admin once.
        /// </summary>
        public async Task LockAfterFailedSignInsAsync(ApplicationUser user, string? remoteIp)
        {
            // A parallel request may have recorded the lock already.
            if (user.LockedAt is not null) return;

            var ok = await ApplyLockAsync(user, FailedSignInReason, lockedByUserId: null, lockedByName: SystemActor);
            if (!ok) return;

            var roles = await _users.GetRolesAsync(user);
            var now = AppClock.Now;
            var who = string.IsNullOrWhiteSpace(user.Email) ? user.FullName : user.Email;

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Email = user.Email ?? string.Empty,
                Category = ActivityLogWriter.SecurityCategory,
                Method = "POST",
                Path = "/api/auth/login",
                Action = "Account locked after failed sign-ins",
                Details = $"{who} ({ActivityLogWriter.CategoryFor(roles)}) was locked after {MaxFailedSignInAttempts} failed sign-in attempts" +
                          (string.IsNullOrWhiteSpace(remoteIp) ? "." : $" (last attempt from {remoteIp})."),
                IsVerified = false,
                CreatedAt = now
            });

            var admins = await _users.GetUsersInRoleAsync(UserRoles.HRAdmin);
            foreach (var admin in admins.Where(a => a.Id != user.Id))
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = "Account locked",
                    Message = Truncate($"{who} was locked after {MaxFailedSignInAttempts} failed sign-in attempts. Review it in User Management.", 500),
                    Module = "Security",
                    Link = "/account.html#users",
                    Type = "AccountLocked",
                    IsRead = false,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning(
                "SECURITY: account {UserId} ({Email}) locked after {Attempts} failed sign-in attempts from {RemoteIp}.",
                user.Id, user.Email, MaxFailedSignInAttempts, remoteIp ?? "unknown");
        }

        /// <summary>HR locks an account. The caller has already refused self-locks.</summary>
        public async Task<bool> LockByAdminAsync(ApplicationUser user, string reason, ClaimsPrincipal admin)
        {
            var adminId = admin.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminName = admin.FindFirstValue(ClaimTypes.Name) ?? "HR Admin";

            if (!await ApplyLockAsync(user, reason.Trim(), adminId, adminName)) return false;

            _logger.LogWarning("SECURITY: account {UserId} ({Email}) locked by HR admin {AdminId}.", user.Id, user.Email, adminId);
            return true;
        }

        /// <summary>Lifts a lock and clears the failed-attempt counter.</summary>
        public async Task<bool> UnlockAsync(ApplicationUser user)
        {
            user.LockedAt = null;
            user.LockedByUserId = null;
            user.LockedByName = null;
            user.LockReason = null;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Could not unlock account {UserId}: {Errors}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Code)));
            }
            return result.Succeeded;
        }

        private async Task<bool> ApplyLockAsync(ApplicationUser user, string reason, string? lockedByUserId, string lockedByName)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.AccessFailedCount = 0;
            user.LockedAt = AppClock.Now;
            user.LockedByUserId = lockedByUserId;
            user.LockedByName = Truncate(lockedByName, 200);
            user.LockReason = Truncate(reason, 500);

            var result = await _users.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Could not lock account {UserId}: {Errors}", user.Id,
                    string.Join(" ", result.Errors.Select(e => e.Code)));
                return false;
            }

            // Ends every session opened before the lock, including after an unlock.
            await _users.UpdateSecurityStampAsync(user);
            return true;
        }

        private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
    }
}

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private static readonly TimeSpan FailedLoginFloor = TimeSpan.FromMilliseconds(800);

        private readonly JwtTokenService _tokens;
        private readonly ApplicationDbContext _context;
        private readonly EmailQueue _emailQueue;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService tokens,
            ApplicationDbContext context,
            EmailQueue emailQueue,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokens = tokens;
            _context = context;
            _emailQueue = emailQueue;
            _logger = logger;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [EnableRateLimiting(RateLimits.AccountEmail)]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // The same reply goes out whether the email is unknown, already
            // has an account, or was accepted, so sign-up cannot be used to
            // find out who is a hired intern or who has an account.
            var accepted = Accepted(new
            {
                message = "If this email belongs to a hired intern without an account, we sent it a verification link. " +
                          "After you verify it, HR must also approve the account before you can sign in. " +
                          "Already have an account? Use Forgot password."
            });

            // Password rules do not depend on the email, so reporting them is safe.
            var probe = new ApplicationUser { UserName = request.Email, Email = request.Email };
            foreach (var validator in _userManager.PasswordValidators)
            {
                var check = await validator.ValidateAsync(_userManager, probe, request.Password);
                if (!check.Succeeded)
                {
                    return BadRequest(new { message = string.Join(" ", check.Errors.Select(e => e.Description)) });
                }
            }

            if (await _userManager.FindByEmailAsync(request.Email) is not null ||
                !await InternRecordExistsAsync(request.Email))
            {
                return accepted;
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Office = request.Office?.Trim(),
                // Knowing a hired intern's email is not proof of identity:
                // the person must verify the inbox AND HR must approve.
                EmailConfirmed = false,
                IsApproved = false
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Self-registration failed: {Errors}",
                    string.Join(" ", result.Errors.Select(e => e.Description)));
                return accepted;
            }

            // New sign-ups are always Interns. HR Admin accounts come only from the seed or from HR.
            await _userManager.AddToRoleAsync(user, UserRoles.Intern);

            await QueueVerificationAsync(user);

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                UserName = user.FullName,
                Email = user.Email ?? string.Empty,
                Category = ActivityLogWriter.CategoryFor(new[] { UserRoles.Intern }),
                Method = "POST",
                Path = "/api/auth/register",
                Action = "Self-registered an intern account",
                Details = "Signed up; waiting for email verification and HR approval in User Management.",
                IsVerified = false,
                CreatedAt = AppClock.Now
            });
            await _context.SaveChangesAsync();

            // No session yet: both verification steps must finish first.
            return accepted;
        }

        /// <summary>Creates the verification token now and sends the mail in the background.</summary>
        private async Task QueueVerificationAsync(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var email = user.Email!;
            var name = user.FullName;
            if (!_emailQueue.Enqueue("verification email",
                    mail => mail.SendEmailConfirmationAsync(email, name, token)))
            {
                _logger.LogWarning("Email queue is full; a verification email was dropped.");
            }
        }

        /// <summary>
        /// POST: api/auth/confirm-email
        /// User side of the two-step verification, from the emailed link.
        /// </summary>
        [HttpPost("confirm-email")]
        [EnableRateLimiting(RateLimits.SignIn)]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            const string invalid = "This verification link is invalid or has expired. Request a new one from the sign-in page.";
            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null) return BadRequest(new { message = invalid });

            // Always validate the token, even for an already-verified account,
            // so this endpoint never reveals an account's status.
            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (!result.Succeeded) return BadRequest(new { message = invalid });

            return Ok(new
            {
                approved = user.IsApproved,
                message = user.IsApproved
                    ? "Email verified. You can now sign in."
                    : "Email verified. Your account is waiting for HR approval; you can sign in once HR approves it."
            });
        }

        /// <summary>
        /// POST: api/auth/resend-confirmation
        /// Always answers the same way so it cannot be used to discover accounts.
        /// </summary>
        [HttpPost("resend-confirmation")]
        [EnableRateLimiting(RateLimits.AccountEmail)]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user is not null && !user.EmailConfirmed && !string.IsNullOrWhiteSpace(user.Email))
            {
                await QueueVerificationAsync(user);
            }

            return Ok(new { message = "If that account still needs verification, a new link has been sent." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [EnableRateLimiting(RateLimits.SignIn)]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Failed sign-ins all take at least this long, so the reply time
            // does not reveal whether the email has an account (a real account
            // also records the failed attempt, which is slower).
            var started = System.Diagnostics.Stopwatch.StartNew();
            async Task<ActionResult<AuthResponse>> InvalidAsync()
            {
                var remaining = FailedLoginFloor - started.Elapsed;
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                _userManager.PasswordHasher.VerifyHashedPassword(
                    TimingDummy.User, TimingDummy.Hash(_userManager.PasswordHasher), request.Password);
                return await InvalidAsync();
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return await InvalidAsync();
            }

            // Two-step verification: the user verified the email and HR
            // approved the account. Checked only after the password, so this
            // reveals nothing to someone guessing emails.
            if (AccountActivation.Blocker(user) is { } blocker)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { code = blocker.Code, message = blocker.Message });
            }

            // Intern access ends after the EndDate on the intern record.
            var loginRoles = await _userManager.GetRolesAsync(user);
            if (loginRoles.Contains(UserRoles.Intern) && !loginRoles.Contains(UserRoles.HRAdmin))
            {
                if (!await InternRecordExistsAsync(user.Email))
                {
                    return Unauthorized(new { message = "This email is not linked to a hired intern record." });
                }

                var endDate = await _tokens.GetInternEndDateAsync(user);
                if (endDate is not null && endDate.Value.Date < AppClock.Now.Date)
                {
                    return Unauthorized(new { message = "Your internship access has expired." });
                }
            }

            await LogLoginAsync(user);

            return await IssueTokenAsync(user);
        }

        /// <summary>POST: api/auth/forgot-password</summary>
        [HttpPost("forgot-password")]
        [EnableRateLimiting(RateLimits.AccountEmail)]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            const string generic = "If that email is registered, a password reset link has been sent.";
            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                return Ok(new { message = generic });
            }

            // Sent in the background and never reported back: a failure or a
            // slower reply here would reveal that the email has an account.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var email = user.Email;
            var name = user.FullName;
            if (!_emailQueue.Enqueue("password reset email",
                    mail => mail.SendPasswordResetLinkAsync(email, name, token)))
            {
                _logger.LogWarning("Email queue is full; a password reset email was dropped.");
            }

            return Ok(new { message = generic });
        }

        /// <summary>POST: api/auth/reset-password</summary>
        [HttpPost("reset-password")]
        [EnableRateLimiting(RateLimits.SignIn)]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return BadRequest(new { message = "New password and confirmation do not match." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired." });
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "This reset link is invalid or has expired. Request a new one from Forgot Password."
                });
            }

            // Opening a reset link from the inbox proves the email is theirs,
            // so it also completes the user's side of verification.
            if (!string.IsNullOrEmpty(user.TemporaryPassword) || !user.EmailConfirmed)
            {
                user.TemporaryPassword = null;
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Category = ActivityLogWriter.CategoryFor(roles),
                    Method = "POST",
                    Path = "/api/auth/reset-password",
                    Action = "Reset a password from email",
                    Details = "Completed a Forgot Password reset.",
                    IsVerified = true,
                    CreatedAt = AppClock.Now
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password was reset but the activity log was not written.");
            }

            return Ok(new { message = "Password updated. You can sign in with the new password." });
        }

        // GET: api/auth/me - needs a session to load the profile.
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> Me()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized(new { message = "Session is no longer valid." });

            return Ok(await BuildProfileAsync(user));
        }

        private async Task LogLoginAsync(ApplicationUser user)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);
                var category = roles.Contains(UserRoles.HRAdmin) ? "HR"
                    : roles.Contains(UserRoles.Supervisor) ? "Supervisor"
                    : "Intern";

                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Category = category,
                    Method = "POST",
                    Path = "/api/auth/login",
                    Action = "Logged in",
                    Details = "Signed in successfully",
                    IsVerified = true,
                    CreatedAt = AppClock.Now
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Do not block the login if the audit log cannot be written.
            }
        }

        private async Task<AuthResponse> IssueTokenAsync(ApplicationUser user)
        {
            var (token, expiresAt) = await _tokens.CreateAsync(user);
            return new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Profile = await BuildProfileAsync(user)
            };
        }

        private async Task<bool> InternRecordExistsAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var key = email.Trim().ToLower();

            // "Completed" interns keep portal access so they can still view
            // their records and documents; only "Hired" interns may punch.
            return await _context.Interns.AsNoTracking()
                .AnyAsync(i => i.Email != null &&
                               i.Email.ToLower() == key &&
                               (i.Status == "Hired" || i.Status == "Completed"));
        }

        private async Task<UserProfile> BuildProfileAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserProfile
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Office = user.Office,
                Roles = roles.ToArray()
            };
        }
    }

    public class JwtSettings
    {
        public string Issuer { get; set; } = "OJTMISApi";
        public string Audience { get; set; } = "OJTMISApiClient";
        public string SigningKey { get; set; } = string.Empty;
        public int ExpiryHours { get; set; } = 8;
    }

    public class RegisterRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Office { get; set; }

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>A throwaway password hash used to equalize login timing.</summary>
    internal static class TimingDummy
    {
        public static readonly ApplicationUser User = new() { Id = "timing-dummy" };
        private static string? _hash;

        public static string Hash(IPasswordHasher<ApplicationUser> hasher) =>
            _hash ??= hasher.HashPassword(User, Guid.NewGuid().ToString("N"));
    }

    public class ConfirmEmailRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class UserProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Office { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserProfile Profile { get; set; } = new UserProfile();
    }
}

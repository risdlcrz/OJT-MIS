using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        private readonly JwtSettings _jwt;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _email;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JwtSettings> jwt,
            ApplicationDbContext context,
            IEmailSender email,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwt = jwt.Value;
            _context = context;
            _email = email;
            _logger = logger;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (await _userManager.FindByEmailAsync(request.Email) is not null)
            {
                return Conflict(new { message = "That email is already registered." });
            }

            if (!await InternRecordExistsAsync(request.Email))
            {
                return BadRequest(new { message = "This email is not listed as a hired intern. Ask HR to create your account." });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Office = request.Office?.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", result.Errors.Select(e => e.Description))
                });
            }

            // Ang bagong account ay Intern lamang. Ang HR Admin ay seed/admin lamang.
            await _userManager.AddToRoleAsync(user, UserRoles.Intern);

            return await IssueTokenAsync(user);
        }

        // POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // Ang intern access ay nag-expire kapag lumipas na ang EndDate
            // na nakasaad sa intern record.
            var loginRoles = await _userManager.GetRolesAsync(user);
            if (loginRoles.Contains(UserRoles.Intern) && !loginRoles.Contains(UserRoles.HRAdmin))
            {
                if (!await InternRecordExistsAsync(user.Email))
                {
                    return Unauthorized(new { message = "This email is not linked to a hired intern record." });
                }

                var endDate = await GetInternEndDateAsync(user);
                if (endDate is not null && endDate.Value.Date < DateTime.Now.Date)
                {
                    return Unauthorized(new { message = "Your internship access has expired." });
                }
            }

            await LogLoginAsync(user);

            return await IssueTokenAsync(user);
        }

        /// <summary>POST: api/auth/forgot-password</summary>
        [HttpPost("forgot-password")]
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

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _email.SendPasswordResetLinkAsync(user.Email, user.FullName, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send a password reset email.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "The password reset email could not be sent. Try again later."
                });
            }

            return Ok(new { message = generic });
        }

        /// <summary>POST: api/auth/reset-password</summary>
        [HttpPost("reset-password")]
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

            if (!string.IsNullOrEmpty(user.TemporaryPassword))
            {
                user.TemporaryPassword = null;
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
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password was reset but the activity log was not written.");
            }

            return Ok(new { message = "Password updated. You can sign in with the new password." });
        }

        // GET: api/auth/me - kailangan ng session para ma-load ang profile.
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
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Huwag harangan ang login kung bigong maisulat ang audit log.
            }
        }

        private async Task<AuthResponse> IssueTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.FullName)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Default token lifetime, pero para sa interns ay hindi lalampas
            // sa EndDate ng kanilang internship.
            var expiresAt = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
            if (roles.Contains(UserRoles.Intern))
            {
                var endDate = await GetInternEndDateAsync(user);
                if (endDate is not null)
                {
                    var internshipEnd = endDate.Value.Date.AddDays(1).ToUniversalTime();
                    if (internshipEnd < expiresAt) expiresAt = internshipEnd;
                }
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                Profile = await BuildProfileAsync(user)
            };
        }

        private async Task<bool> InternRecordExistsAsync(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var key = email.Trim().ToLower();
            return await _context.Interns.AsNoTracking()
                .AnyAsync(i => i.Email != null && i.Email.ToLower() == key && i.Status == "Hired");
        }

        private async Task<DateTime?> GetInternEndDateAsync(ApplicationUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) return null;

            var email = user.Email.Trim().ToLower();
            return await _context.Interns
                .AsNoTracking()
                .Where(i => i.Email != null &&
                            i.Email.ToLower() == email &&
                            i.EndDate != null)
                .OrderByDescending(i => i.EndDate)
                .Select(i => i.EndDate)
                .FirstOrDefaultAsync();
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

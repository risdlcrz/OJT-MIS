using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/account")]
    [Authorize]
    [Produces("application/json")]
    public class AccountController : ControllerBase
    {
        // A 256x256 JPEG data URL is well under this. The cap only stops
        // oversized payloads from reaching the database.
        private const int MaxAvatarLength = 700_000;

        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>GET: api/account/profile</summary>
        [HttpGet("profile")]
        public async Task<ActionResult<AccountProfile>> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized(new { message = "Session is no longer valid." });

            return Ok(await BuildProfileAsync(user));
        }

        /// <summary>PUT: api/account/profile</summary>
        [HttpPut("profile")]
        public async Task<ActionResult<AccountProfile>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (request.AvatarUrl is { Length: > MaxAvatarLength })
            {
                return BadRequest(new { message = "Avatar image is too large. Please choose a smaller image." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized(new { message = "Session is no longer valid." });

            var email = request.Email.Trim();

            if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing is not null && existing.Id != user.Id)
                {
                    return Conflict(new { message = "That email is already registered." });
                }

                // Email is also the username in this application.
                user.Email = email;
                user.UserName = email;
            }

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Office = string.IsNullOrWhiteSpace(request.Office) ? null : request.Office.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", result.Errors.Select(e => e.Description))
                });
            }

            return Ok(await BuildProfileAsync(user));
        }

        /// <summary>POST: api/account/change-password</summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return BadRequest(new { message = "New password and confirmation do not match." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized(new { message = "Session is no longer valid." });

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", result.Errors.Select(e => e.Description))
                });
            }

            return Ok(new { message = "Password changed successfully." });
        }

        private async Task<AccountProfile> BuildProfileAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new AccountProfile
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Office = user.Office,
                AvatarUrl = user.AvatarUrl,
                Roles = roles.ToArray(),
                CreatedAt = user.CreatedAt
            };
        }
    }

    public class AccountProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Office { get; set; }
        public string? AvatarUrl { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Office { get; set; }

        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

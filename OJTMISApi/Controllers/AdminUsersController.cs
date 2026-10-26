using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Models;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>GET: api/admin/users?role=Intern</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAll([FromQuery] string? role)
        {
            var roleFilter = role?.Trim();
            var filterRole = NormalizeRole(roleFilter);

            if (!string.IsNullOrWhiteSpace(roleFilter) &&
                !string.Equals(roleFilter, "all", StringComparison.OrdinalIgnoreCase) &&
                filterRole is null)
            {
                return BadRequest(new { message = $"Role must be one of: {string.Join(", ", UserRoles.All)}." });
            }

            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            var result = new List<AdminUserDto>(users.Count);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (filterRole is not null && !roles.Contains(filterRole, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(ToDto(user, roles));
            }

            return Ok(result);
        }

        /// <summary>POST: api/admin/users</summary>
        [HttpPost]
        public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateAdminUserRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var role = NormalizeRole(request.Role);
            if (role is null)
            {
                return BadRequest(new { message = $"Role must be one of: {string.Join(", ", UserRoles.All)}." });
            }

            var email = request.Email.Trim();

            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                return Conflict(new { message = "That email is already registered." });
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Office = string.IsNullOrWhiteSpace(request.Office) ? null : request.Office.Trim(),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", createResult.Errors.Select(e => e.Description))
                });
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                // Huwag mag-iwan ng account na walang role kung nabigo ang role assignment.
                await _userManager.DeleteAsync(user);

                return BadRequest(new
                {
                    message = string.Join(" ", roleResult.Errors.Select(e => e.Description))
                });
            }

            return Created($"/api/admin/users/{user.Id}", ToDto(user, new[] { role }));
        }

        /// <summary>PUT: api/admin/users/{id}</summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<AdminUserDto>> Update(string id, [FromBody] UpdateAdminUserRequest request)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound(new { message = $"User with id {id} was not found." });
            }

            var role = NormalizeRole(request.Role);
            if (role is null)
            {
                return BadRequest(new { message = $"Role must be one of: {string.Join(", ", UserRoles.All)}." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId && !string.Equals(role, UserRoles.HRAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "You cannot remove your own HR Admin access." });
            }

            if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 8)
            {
                return BadRequest(new { message = "Password must be at least 8 characters long." });
            }

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

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = string.Join(" ", updateResult.Errors.Select(e => e.Description))
                });
            }

            // Optional password reset when the admin typed a new password.
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, resetToken, request.Password);
                if (!passwordResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = string.Join(" ", passwordResult.Errors.Select(e => e.Description))
                    });
                }
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                // Idagdag muna ang bagong role bago tanggalin ang luma, para
                // hindi maiwang walang role ang account kung may error.
                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = string.Join(" ", addResult.Errors.Select(e => e.Description))
                    });
                }

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = string.Join(" ", removeResult.Errors.Select(e => e.Description))
                    });
                }
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(ToDto(user, roles));
        }

        private static AdminUserDto ToDto(ApplicationUser user, IEnumerable<string> roles)
        {
            return new AdminUserDto
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

        /// <summary>
        /// Returns the canonical role name, or null when the value is not a
        /// supported role. A null ("all") is treated as "no filter".
        /// </summary>
        private static string? NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return null;
            if (string.Equals(role.Trim(), "all", StringComparison.OrdinalIgnoreCase)) return null;

            return UserRoles.All.FirstOrDefault(
                candidate => string.Equals(candidate, role.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public class AdminUserDto
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

    public class CreateAdminUserRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Office { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateAdminUserRequest
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50, MinimumLength = 2)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Office { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        /// <summary>Leave blank to keep the current password.</summary>
        [StringLength(100)]
        public string? Password { get; set; }
    }
}

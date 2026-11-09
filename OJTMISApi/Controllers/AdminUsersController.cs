using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _email;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IEmailService email)
        {
            _userManager = userManager;
            _context = context;
            _email = email;
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

        /// <summary>GET: api/admin/users/available-people</summary>
        [HttpGet("available-people")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailablePeople()
        {
            var existingEmails = await _userManager.Users
                .Where(u => u.Email != null)
                .Select(u => u.Email!.ToLower())
                .ToListAsync();

            var emailSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var interns = await _context.Interns.AsNoTracking()
                .Where(i => i.Email != null && i.Email != "")
                .OrderBy(i => i.FullName)
                .ToListAsync();

            var internPeople = interns
                .Where(i => !emailSet.Contains(i.Email.ToLower()))
                .Select(i => new
                {
                    source = "Intern",
                    id = i.Id,
                    fullName = i.FullName,
                    email = i.Email,
                    office = i.ApplicantOffice,
                    role = UserRoles.Intern
                });

            var signatories = await _context.Signatories.AsNoTracking()
                .Where(s => s.Email != null && s.Email != "")
                .OrderBy(s => s.Name)
                .ToListAsync();

            var signatoryPeople = signatories
                .Where(s => !emailSet.Contains(s.Email.ToLower()))
                .Select(s => new
                {
                    source = "Signatory",
                    id = s.Id,
                    fullName = s.Name,
                    email = s.Email,
                    office = s.Department,
                    role = UserRoles.Supervisor
                });

            return Ok(internPeople.Concat(signatoryPeople).OrderBy(p => p.fullName).ToList());
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

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "HR cannot set a password directly. A temporary password is emailed to the user." });
            }

            var email = request.Email.Trim();

            if (await _userManager.FindByEmailAsync(email) is not null)
            {
                return Conflict(new { message = "That email is already registered." });
            }

            var password = TemporaryPasswordGenerator.Generate();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Office = string.IsNullOrWhiteSpace(request.Office) ? null : request.Office.Trim(),
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
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

            try
            {
                await _email.SendTemporaryPasswordAsync(email, user.FullName, password);
            }
            catch (Exception)
            {
                await _userManager.DeleteAsync(user);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = "The account was not created because the temporary password email could not be sent."
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

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "HR cannot change a password directly. The user can reset it from Forgot Password or Account > Security." });
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

        /// <summary>
        /// POST: api/admin/users/{id}/password-reset
        /// HR cannot choose the new password. A reset link is emailed instead.
        /// </summary>
        [HttpPost("{id}/password-reset")]
        public async Task<IActionResult> SendPasswordReset(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                return NotFound(new { message = $"User with id {id} was not found." });
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return BadRequest(new { message = "This account has no email address to send a reset link to." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            try
            {
                await _email.SendPasswordResetLinkAsync(user.Email, user.FullName, token);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    message = ex.Message
                });
            }

            ActivityLogWriter.Add(
                _context,
                User,
                "POST",
                $"/api/admin/users/{id}/password-reset",
                "Emailed a password reset link",
                $"Sent a password reset link to {user.Email}.");
            await _context.SaveChangesAsync();

            return Ok(new { message = $"A password reset link was emailed to {user.Email}." });
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
                EmailConfirmed = user.EmailConfirmed,
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
        public bool EmailConfirmed { get; set; }
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

        /// <summary>Ignored. HR cannot choose a password; one is emailed instead.</summary>
        public string? Password { get; set; }
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

        /// <summary>Not accepted. Password changes go through Forgot Password or Account > Security.</summary>
        public string? Password { get; set; }
    }
}

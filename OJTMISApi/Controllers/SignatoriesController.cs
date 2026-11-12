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
    [Route("api/[controller]")]
    [Authorize(Roles = UserRoles.HRAdmin)]
    [Produces("application/json")]
    public class SignatoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SignatoriesController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _email;

        public SignatoriesController(
            ApplicationDbContext context,
            ILogger<SignatoriesController> logger,
            UserManager<ApplicationUser> userManager,
            IEmailService email)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _email = email;
        }

        /// <summary>GET: api/signatories</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Signatory>>> GetAll()
        {
            try
            {
                var items = await _context.Signatories
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .ToListAsync();
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Signatories.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving Signatories.", statusCode: 500);
            }
        }

        /// <summary>GET: api/signatories/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Signatory>> GetById(int id)
        {
            try
            {
                var item = await _context.Signatories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (item is null) return NotFound(new { message = "Signatory with id {id} was not found." });
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while retrieving the record.", statusCode: 500);
            }
        }

        /// <summary>POST: api/signatories</summary>
        [HttpPost]
        public async Task<ActionResult<Signatory>> Create([FromBody] Signatory item)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.Name?.Trim() ?? string.Empty;
            var f2 = item.Position?.Trim() ?? string.Empty;
            var f3 = item.Department?.Trim() ?? string.Empty;
            var email = item.Email?.Trim() ?? string.Empty;
            var scope = SignatoryApprovalScope.Normalize(item.ApprovalScope);

            if (!string.IsNullOrWhiteSpace(item.UserId) &&
                await _userManager.FindByIdAsync(item.UserId.Trim()) is null)
            {
                return BadRequest(new { message = "The selected user account was not found." });
            }

            if (await _context.Signatories.AnyAsync(x => x.Name != null && x.Name.ToLower() == f1.ToLower()))
            {
                return Conflict(new { message = "A record with the same first field already exists." });
            }

            var now = DateTime.Now;
            var entity = new Signatory
            {
                Name = f1,
                Position = f2,
                Department = f3,
                Email = email,
                UserId = await ResolveUserIdAsync(item.UserId, email),
                ApprovalScope = scope,
                CreatedAt = now,
                UpdatedAt = now
            };

            try
            {
                _context.Signatories.Add(entity);
                await _context.SaveChangesAsync();

                // Awtomatikong gumawa ng unverified account para makita agad
                // sa User Management at mabigyan ng access ng HR.
                await ProvisionAccountAsync(entity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Signatory.");
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while creating the record.", statusCode: 500);
            }
        }

        /// <summary>PUT: api/signatories/{id}</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Signatory>> Update(int id, [FromBody] Signatory item)
        {
            if (id != item.Id) return BadRequest(new { message = "The id in the URL does not match the id in the body." });
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var f1 = item.Name?.Trim() ?? string.Empty;
            var f2 = item.Position?.Trim() ?? string.Empty;
            var f3 = item.Department?.Trim() ?? string.Empty;
            var email = item.Email?.Trim() ?? string.Empty;
            var scope = SignatoryApprovalScope.Normalize(item.ApprovalScope);

            if (!string.IsNullOrWhiteSpace(item.UserId) &&
                await _userManager.FindByIdAsync(item.UserId.Trim()) is null)
            {
                return BadRequest(new { message = "The selected user account was not found." });
            }

            try
            {
                var existing = await _context.Signatories.FirstOrDefaultAsync(x => x.Id == id);
                if (existing is null) return NotFound(new { message = "Signatory with id {id} was not found." });

                if (await _context.Signatories.AnyAsync(x => x.Id != id && x.Name != null && x.Name.ToLower() == f1.ToLower()))
                {
                    return Conflict(new { message = "A record with the same first field already exists." });
                }

                var userId = await ResolveUserIdAsync(item.UserId, email);
                if (userId is not null &&
                    await _context.Signatories.AnyAsync(x => x.Id != id && x.UserId == userId))
                {
                    return Conflict(new { message = "That user account is already linked to another signatory." });
                }

                existing.Name = f1;
                existing.Position = f2;
                existing.Department = f3;
                existing.Email = email;
                existing.UserId = userId;
                existing.ApprovalScope = scope;
                existing.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await ProvisionAccountAsync(existing);
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while updating the record.", statusCode: 500);
            }
        }

        /// <summary>
        /// Finds the credential a signatory should be linked to: the explicit
        /// user id first, then an existing account with the same email.
        /// </summary>
        private async Task<string?> ResolveUserIdAsync(string? userId, string? email)
        {
            var id = userId?.Trim();
            if (!string.IsNullOrWhiteSpace(id) && await _userManager.FindByIdAsync(id) is not null)
            {
                return id;
            }

            var address = email?.Trim();
            if (!string.IsNullOrWhiteSpace(address) && address.Contains('@'))
            {
                return (await _userManager.FindByEmailAsync(address))?.Id;
            }

            return null;
        }

        /// <summary>
        /// Creates an unverified user account for a signatory when one does not
        /// exist and links the signatory record to that account. The temporary
        /// password is emailed; HR cannot type it.
        /// </summary>
        private async Task ProvisionAccountAsync(Signatory signatory)
        {
            var email = signatory.Email?.Trim();

            ApplicationUser? user = null;

            if (!string.IsNullOrWhiteSpace(signatory.UserId))
            {
                user = await _userManager.FindByIdAsync(signatory.UserId);
            }

            if (user is null && !string.IsNullOrWhiteSpace(email) && email.Contains('@'))
            {
                user = await _userManager.FindByEmailAsync(email);
            }

            if (user is null)
            {
                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return;

                try
                {
                    var nameParts = signatory.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstName = nameParts.Length > 1 ? string.Join(' ', nameParts[..^1]) : signatory.Name.Trim();
                    var lastName = nameParts.Length > 1 ? nameParts[^1] : string.Empty;

                    var isHr = signatory.Department.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                               signatory.Position.Contains("HR", StringComparison.OrdinalIgnoreCase) ||
                               signatory.Department.Contains("Human Resource", StringComparison.OrdinalIgnoreCase);

                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        Office = string.IsNullOrWhiteSpace(signatory.Department) ? null : signatory.Department,
                        EmailConfirmed = false
                    };

                    var password = TemporaryPasswordGenerator.Generate();
                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        return;
                    }

                    await _userManager.AddToRoleAsync(user, isHr ? UserRoles.HRAdmin : UserRoles.Supervisor);
                    try
                    {
                        await _email.SendTemporaryPasswordAsync(email, user.FullName, password);
                    }
                    catch (Exception mailEx)
                    {
                        _logger.LogWarning(mailEx, "Signatory account for {Email} was created but the password email failed.", email);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not auto-create user account for signatory {Name}.", signatory.Name);
                    return;
                }
            }

            // Bind the directory entry to the credential so the officer can be
            // resolved in the certificate and DTR approval workflows.
            signatory.UserId = user.Id;

            if (string.IsNullOrWhiteSpace(user.Office) && !string.IsNullOrWhiteSpace(signatory.Department))
            {
                user.Office = signatory.Department.Trim();
                await _userManager.UpdateAsync(user);
            }
        }

        /// <summary>DELETE: api/signatories/{id}</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var entity = await _context.Signatories.FindAsync(id);
                if (entity is null) return NotFound(new { message = "Signatory with id {id} was not found." });

                _context.Signatories.Remove(entity);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Signatory {Id}.", id);
                return Problem(title: "Internal Server Error", detail: "An unexpected error occurred while deleting the record.", statusCode: 500);
            }
        }
    }
}



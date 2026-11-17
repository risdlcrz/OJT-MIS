using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OJTMISApi.Controllers;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Issues session tokens and re-checks them on every request.
    ///
    /// Each token carries the user's Identity security stamp. Identity
    /// changes the stamp whenever the password is changed or reset, so every
    /// token issued before that stops working on its next request. Every
    /// request also re-reads the account, so a locked, unapproved or
    /// unverified account is cut off at once, and role / email / name changes
    /// take effect without signing in again.
    /// </summary>
    public class JwtTokenService
    {
        public const string SecurityStampClaim = "sstamp";

        private readonly UserManager<ApplicationUser> _users;
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwt;

        public JwtTokenService(
            UserManager<ApplicationUser> users,
            ApplicationDbContext context,
            IOptions<JwtSettings> jwt)
        {
            _users = users;
            _context = context;
            _jwt = jwt.Value;
        }

        public async Task<(string Token, DateTime ExpiresAt)> CreateAsync(ApplicationUser user)
        {
            var roles = await _users.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new(ClaimTypes.Name, user.FullName),
                new(SecurityStampClaim, user.SecurityStamp ?? string.Empty)
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

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public async Task<DateTime?> GetInternEndDateAsync(ApplicationUser user)
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

        /// <summary>
        /// JwtBearer OnTokenValidated hook: rejects the token unless the account
        /// still exists, its security stamp matches, it is not locked out, and
        /// both verification steps are complete. Refreshes the identity claims
        /// from the database.
        /// </summary>
        public static async Task ValidateAsync(TokenValidatedContext context)
        {
            var principal = context.Principal;
            var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = principal?.FindFirstValue(SecurityStampClaim);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stamp))
            {
                context.Fail("Session token is missing required claims.");
                return;
            }

            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByIdAsync(userId);
            if (user is null ||
                !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal) ||
                await users.IsLockedOutAsync(user) ||
                AccountActivation.Blocker(user) is not null)
            {
                context.Fail("Session is no longer valid.");
                return;
            }

            var roles = await users.GetRolesAsync(user);
            var fresh = new ClaimsIdentity(
                principal!.Claims.Where(c =>
                    c.Type != ClaimTypes.Role &&
                    c.Type != ClaimTypes.Email &&
                    c.Type != ClaimTypes.Name),
                principal.Identity?.AuthenticationType ?? JwtBearerDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role);
            fresh.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
            fresh.AddClaim(new Claim(ClaimTypes.Name, user.FullName));
            foreach (var role in roles) fresh.AddClaim(new Claim(ClaimTypes.Role, role));

            context.Principal = new ClaimsPrincipal(fresh);
        }
    }
}

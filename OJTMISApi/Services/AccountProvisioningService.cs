using Microsoft.AspNetCore.Identity;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>What happened when an account was provisioned.</summary>
    public enum ProvisionStatus
    {
        /// <summary>A new account was created and the activation email was sent.</summary>
        Created,
        /// <summary>A new account was created but the activation email failed.</summary>
        CreatedEmailFailed,
        /// <summary>An account with that email already existed (returned in <see cref="ProvisionResult.User"/>).</summary>
        AlreadyExists,
        /// <summary>The email address is missing or not usable.</summary>
        InvalidEmail,
        /// <summary>Identity rejected the account or role (see <see cref="ProvisionResult.Error"/>).</summary>
        Failed,
        /// <summary>The activation email failed and the new account was removed again.</summary>
        RolledBack
    }

    public sealed record ProvisionResult(ProvisionStatus Status, ApplicationUser? User, string? Error = null)
    {
        public bool Created => Status is ProvisionStatus.Created or ProvisionStatus.CreatedEmailFailed;
    }

    /// <summary>
    /// The single place HR-initiated accounts are created (User Management,
    /// hiring, adding an intern, signatories). Every such account:
    /// <list type="bullet">
    /// <item>is approved (HR created it) but not email-verified;</item>
    /// <item>gets a random temporary password that HR never sees;</item>
    /// <item>receives the activation email with the verification link.</item>
    /// </list>
    /// </summary>
    public sealed class AccountProvisioningService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailService _email;
        private readonly ILogger<AccountProvisioningService> _logger;

        public AccountProvisioningService(
            UserManager<ApplicationUser> users,
            IEmailService email,
            ILogger<AccountProvisioningService> logger)
        {
            _users = users;
            _email = email;
            _logger = logger;
        }

        /// <summary>
        /// Creates the account unless one already exists for the email.
        /// </summary>
        /// <param name="email">Login email; also where the activation mail goes.</param>
        /// <param name="firstName">First name shown in the app.</param>
        /// <param name="lastName">Last name shown in the app.</param>
        /// <param name="office">Office/department, or null.</param>
        /// <param name="role">One of <see cref="UserRoles"/>.</param>
        /// <param name="rollbackIfEmailFails">
        /// Delete the new account again when the activation email cannot be
        /// sent (User Management does this so HR can simply retry).
        /// </param>
        public async Task<ProvisionResult> CreateAsync(
            string? email,
            string firstName,
            string lastName,
            string? office,
            string role,
            bool rollbackIfEmailFails = false)
        {
            email = email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                return new(ProvisionStatus.InvalidEmail, null, "No valid email address.");
            }

            var existing = await _users.FindByEmailAsync(email);
            if (existing is not null)
            {
                return new(ProvisionStatus.AlreadyExists, existing);
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Office = string.IsNullOrWhiteSpace(office) ? null : office.Trim(),
                IsApproved = true,
                EmailConfirmed = false
            };

            var password = TemporaryPasswordGenerator.Generate();
            var created = await _users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                var error = string.Join(" ", created.Errors.Select(e => e.Description));
                _logger.LogWarning("Could not create a {Role} account: {Errors}", role, error);
                return new(ProvisionStatus.Failed, null, error);
            }

            var roleResult = await _users.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                // Never leave an account without a role behind.
                await _users.DeleteAsync(user);
                return new(ProvisionStatus.Failed, null,
                    string.Join(" ", roleResult.Errors.Select(e => e.Description)));
            }

            try
            {
                await _email.SendActivationAsync(_users, user, password);
                return new(ProvisionStatus.Created, user);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "A {Role} account was created but the activation email failed.", role);

                if (rollbackIfEmailFails)
                {
                    await _users.DeleteAsync(user);
                    return new(ProvisionStatus.RolledBack, null, "The activation email could not be sent.");
                }

                return new(ProvisionStatus.CreatedEmailFailed, user, "The activation email could not be sent.");
            }
        }
    }
}

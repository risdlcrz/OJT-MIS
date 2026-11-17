using Microsoft.AspNetCore.Identity;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Two-step account verification shared by every place that creates or
    /// signs in an account:
    /// <list type="number">
    /// <item>HR approval - <see cref="ApplicationUser.IsApproved"/>. Set when
    /// HR creates the account (hiring, User Management, signatories) or when HR
    /// approves a self-registered account.</item>
    /// <item>User verification - <see cref="IdentityUser.EmailConfirmed"/>.
    /// Set when the user opens the link emailed to them, proving the email
    /// address (first given as an applicant) really belongs to them.</item>
    /// </list>
    /// </summary>
    public static class AccountActivation
    {
        public const string EmailUnconfirmedCode = "email_unconfirmed";
        public const string PendingApprovalCode = "pending_approval";

        /// <summary>Emails the temporary password together with the verification link.</summary>
        public static async Task SendActivationAsync(
            this IEmailService email,
            UserManager<ApplicationUser> users,
            ApplicationUser user,
            string temporaryPassword)
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            await email.SendTemporaryPasswordAsync(user.Email!, user.FullName, temporaryPassword, token);
        }

        /// <summary>Emails only the verification link (self-registration and "resend").</summary>
        public static async Task SendVerificationAsync(
            this IEmailService email,
            UserManager<ApplicationUser> users,
            ApplicationUser user)
        {
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            await email.SendEmailConfirmationAsync(user.Email!, user.FullName, token);
        }

        /// <summary>
        /// Why this account may not sign in yet, or null when both steps are
        /// done. Only call after the password has been checked, so the reply
        /// never reveals anything to someone who does not know the password.
        /// </summary>
        public static (string Code, string Message)? Blocker(ApplicationUser user)
        {
            if (!user.EmailConfirmed)
            {
                return (EmailUnconfirmedCode,
                    "Verify your email first. Open the link we emailed you, or request a new one below.");
            }

            if (!user.IsApproved)
            {
                return (PendingApprovalCode,
                    "Your email is verified. Your account is waiting for HR approval before you can sign in.");
            }

            return null;
        }
    }
}

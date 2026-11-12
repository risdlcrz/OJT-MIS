using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OJTMISApi.Models
{
    /// <summary>
    /// Approval scope of a signatory record.
    /// </summary>
    public static class SignatoryApprovalScope
    {
        public const string Certificate = "Certificate";
        public const string Dtr = "DTR";
        public const string Both = "Both";

        public static readonly string[] All = { Certificate, Dtr, Both };

        /// <summary>
        /// Missing or unknown scopes fall back to <see cref="Both"/> so existing
        /// signatory records keep working after the approval columns are added.
        /// </summary>
        public static string Normalize(string? value)
        {
            var text = (value ?? string.Empty).Trim();

            foreach (var scope in All)
            {
                if (text.Equals(scope, StringComparison.OrdinalIgnoreCase))
                {
                    return scope;
                }
            }

            return Both;
        }

        public static bool AllowsCertificates(string? value)
        {
            var scope = Normalize(value);
            return scope is Both or Certificate;
        }

        public static bool AllowsDtr(string? value)
        {
            var scope = Normalize(value);
            return scope is Both or Dtr;
        }
    }

    /// <summary>
    /// A designated approving officer. The record is linked to the officer's
    /// login account (<see cref="UserId"/>) and scoped to a department so the
    /// same person can be reused by the Certificate of Completion and DTR
    /// approval workflows.
    /// </summary>
    public class Signatory
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identity account that approves/signs on behalf of this record.
        /// Null for directories imported before the account was provisioned.
        /// </summary>
        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Position is required.")]
        [StringLength(150, ErrorMessage = "Position cannot exceed 150 characters.")]
        public string Position { get; set; } = string.Empty;

        /// <summary>
        /// Department scope. Certificate and DTR approvals are matched against
        /// the intern's office using this value.
        /// </summary>
        [StringLength(200, ErrorMessage = "Department cannot exceed 200 characters.")]
        public string Department { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Which workflow may use this officer:
        /// <see cref="SignatoryApprovalScope.Certificate"/>,
        /// <see cref="SignatoryApprovalScope.Dtr"/>, or
        /// <see cref="SignatoryApprovalScope.Both"/>.
        /// </summary>
        [Required, StringLength(20)]
        public string ApprovalScope { get; set; } = SignatoryApprovalScope.Both;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}

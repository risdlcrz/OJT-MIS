using System.Security.Cryptography;
using System.Text;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Attendance code printed on the intern ID: "OJT2." followed by 256
    /// random bits. The code is opaque - scanning it with any phone app shows
    /// only random characters, never an intern number or name - and it is
    /// stored on the intern record, so:
    /// <list type="bullet">
    /// <item>no two interns (or two ID issuances) ever share a code;</item>
    /// <item>generating a new ID replaces the code and cancels old cards;</item>
    /// <item>a code is accepted only from the account of the intern it was issued to.</item>
    /// </list>
    /// The earlier "OJT1.{internId}.{signature}" format exposed the intern id
    /// and could not be revoked, so it is no longer accepted.
    /// </summary>
    public class AttendanceQrService
    {
        public const string Prefix = "OJT2";

        /// <summary>Creates a new random, URL-safe attendance code.</summary>
        public string NewToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var body = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"{Prefix}.{body}";
        }

        /// <summary>
        /// True when <paramref name="payload"/> is exactly the code currently
        /// issued to the intern. Compared in constant time.
        /// </summary>
        public bool Matches(string? payload, string? issuedToken)
        {
            if (string.IsNullOrEmpty(issuedToken)) return false;

            var scanned = (payload ?? string.Empty).Trim();
            if (!scanned.StartsWith(Prefix + ".", StringComparison.Ordinal)) return false;

            var a = Encoding.UTF8.GetBytes(scanned);
            var b = Encoding.UTF8.GetBytes(issuedToken);
            return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>True when the payload is an old OJT1 code from an earlier ID.</summary>
        public static bool IsLegacy(string? payload) =>
            (payload ?? string.Empty).Trim().StartsWith("OJT1.", StringComparison.Ordinal);
    }
}

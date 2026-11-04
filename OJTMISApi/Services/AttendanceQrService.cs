using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OJTMISApi.Controllers;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Signed attendance payload printed on the intern ID: OJT1.{internId}.{hmac}.
    /// The signature uses the API signing key so a scanned code can be checked
    /// without storing a separate token column.
    /// </summary>
    public class AttendanceQrService
    {
        public const string Prefix = "OJT1";
        private readonly byte[] _key;

        public AttendanceQrService(IOptions<JwtSettings> jwt)
        {
            var signingKey = jwt.Value.SigningKey;
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                throw new InvalidOperationException("Jwt:SigningKey is required to sign attendance QR codes.");
            }

            _key = Encoding.UTF8.GetBytes(signingKey);
        }

        public string Create(int internId)
        {
            var body = $"{Prefix}.{internId}";
            return $"{body}.{Sign(body)}";
        }

        public bool TryRead(string? payload, out int internId)
        {
            internId = 0;
            var parts = (payload ?? string.Empty).Trim().Split('.');
            if (parts.Length != 3 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out internId) || internId <= 0)
            {
                return false;
            }

            var expected = Sign($"{parts[0]}.{parts[1]}");
            var actual = parts[2];
            if (expected.Length != actual.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(actual));
        }

        private string Sign(string body)
        {
            var hash = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(body));
            return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}

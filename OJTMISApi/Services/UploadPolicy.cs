using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Upload limits shared by the API and the Vue client (the client reads
    /// them from <c>GET api/uploads/policy</c>). Configured under
    /// <c>Uploads</c> in appsettings.json. There is one rule per place that
    /// accepts a file:
    /// <list type="bullet">
    /// <item><c>Avatar</c>: profile picture, sent to the API as a data URL and stored.</item>
    /// <item><c>IdPhoto</c> / <c>IdSignature</c>: images on the ID generator page. They are
    /// only drawn into the printed card in the browser and never sent to the
    /// API, so only the client applies them.</item>
    /// </list>
    /// </summary>
    public sealed class UploadOptions
    {
        public const string SectionName = "Uploads";

        public UploadRule Avatar { get; set; } = new();
        public UploadRule IdPhoto { get; set; } = new();
        public UploadRule IdSignature { get; set; } = new();

        public IEnumerable<(string Name, UploadRule Rule)> All()
        {
            yield return (nameof(Avatar), Avatar);
            yield return (nameof(IdPhoto), IdPhoto);
            yield return (nameof(IdSignature), IdSignature);
        }

        /// <summary>Startup check: every rule must be complete and use only supported image types.</summary>
        public bool IsValid() => All().All(entry => entry.Rule.IsValid());
    }

    public sealed class UploadRule
    {
        /// <summary>Largest accepted file, in bytes (the decoded image on the server).</summary>
        public long MaxBytes { get; set; }

        /// <summary>How many files one upload may carry.</summary>
        public int MaxFiles { get; set; }

        /// <summary>MIME types, for example <c>image/jpeg</c>. Must be in <see cref="UploadValidator.KnownSignatures"/>.</summary>
        public string[] AllowedContentTypes { get; set; } = Array.Empty<string>();

        /// <summary>File name extensions with the dot, for example <c>.jpg</c>.</summary>
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();

        public bool IsValid() =>
            MaxBytes > 0 &&
            MaxFiles > 0 &&
            AllowedContentTypes.Length > 0 &&
            AllowedExtensions.Length > 0 &&
            AllowedContentTypes.All(type => UploadValidator.KnownSignatures.ContainsKey(type.ToLowerInvariant())) &&
            AllowedExtensions.All(ext => ext.StartsWith('.'));

        /// <summary>Largest request body that can carry MaxFiles files as base64 data URLs, plus room for the other form fields.</summary>
        public long MaxRequestBytes => (MaxBytes * 4 / 3 + 256) * MaxFiles + 64 * 1024;
    }

    /// <summary>
    /// Checks a <c>data:image/...;base64,</c> URL against an <see cref="UploadRule"/>:
    /// declared MIME type allowed, base64 valid, decoded size under the cap, and
    /// the file's leading bytes really are that image type.
    /// </summary>
    public static class UploadValidator
    {
        /// <summary>Magic bytes for each supported image type. WebP is "RIFF????WEBP".</summary>
        public static readonly IReadOnlyDictionary<string, Func<byte[], bool>> KnownSignatures =
            new Dictionary<string, Func<byte[], bool>>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = b => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
                ["image/png"] = b => b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
                                     b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A,
                ["image/webp"] = b => b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
                                      b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50
            };

        /// <summary>Returns a short user-facing error, or null when the data URL is acceptable.</summary>
        public static string? ValidateDataUrl(string dataUrl, UploadRule rule)
        {
            const string invalid = "The image could not be read. Choose a different file and try again.";

            if (!dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return invalid;

            var comma = dataUrl.IndexOf(',');
            if (comma < 0) return invalid;

            var header = dataUrl[5..comma]; // e.g. "image/jpeg;base64"
            const string base64Suffix = ";base64";
            if (!header.EndsWith(base64Suffix, StringComparison.OrdinalIgnoreCase)) return invalid;

            var contentType = header[..^base64Suffix.Length].Trim().ToLowerInvariant();
            if (!rule.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            {
                return $"This file type is not allowed. Use {Describe(rule)}.";
            }

            var payload = dataUrl.AsSpan(comma + 1);

            // Reject before decoding when the text alone is already too large.
            if (payload.Length / 4L * 3 > rule.MaxBytes + 3)
            {
                return $"The image is too large. The limit is {FormatBytes(rule.MaxBytes)}.";
            }

            var buffer = new byte[payload.Length / 4 * 3 + 3];
            if (!Convert.TryFromBase64Chars(payload, buffer, out var written)) return invalid;
            if (written == 0) return invalid;
            if (written > rule.MaxBytes)
            {
                return $"The image is too large. The limit is {FormatBytes(rule.MaxBytes)}.";
            }

            var bytes = written == buffer.Length ? buffer : buffer[..written];
            if (!KnownSignatures.TryGetValue(contentType, out var matches) || !matches(bytes))
            {
                return $"The file is not a valid image. Use {Describe(rule)}.";
            }

            return null;
        }

        public static string Describe(UploadRule rule)
        {
            var names = rule.AllowedExtensions
                .Select(ext => ext.TrimStart('.').ToUpperInvariant())
                .Select(ext => ext == "JPEG" ? "JPG" : ext)
                .Distinct()
                .ToList();
            var list = names.Count <= 1
                ? string.Join("", names)
                : $"{string.Join(", ", names.Take(names.Count - 1))} or {names[^1]}";
            return $"{list}, up to {FormatBytes(rule.MaxBytes)}";
        }

        public static string FormatBytes(long bytes) =>
            bytes >= 1024 * 1024 && bytes % (1024 * 1024) == 0 ? $"{bytes / (1024 * 1024)} MB"
            : bytes >= 1024 * 1024 ? $"{bytes / (1024d * 1024):0.#} MB"
            : $"{Math.Max(1, bytes / 1024)} KB";
    }

    /// <summary>
    /// Caps the request body for an action that accepts an upload, using the
    /// configured rule. Runs before model binding reads the body, so an
    /// oversized request is refused with 413 instead of being buffered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UploadRequestLimitAttribute : Attribute, IResourceFilter
    {
        private readonly string _rule;

        /// <param name="rule">Property name on <see cref="UploadOptions"/>, for example <c>nameof(UploadOptions.Avatar)</c>.</param>
        public UploadRequestLimitAttribute(string rule) => _rule = rule;

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<UploadOptions>>().Value;
            var rule = options.All().FirstOrDefault(entry => entry.Name == _rule).Rule
                ?? throw new InvalidOperationException($"Unknown upload rule '{_rule}'.");

            var feature = context.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (feature is { IsReadOnly: false })
            {
                feature.MaxRequestBodySize = rule.MaxRequestBytes;
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}

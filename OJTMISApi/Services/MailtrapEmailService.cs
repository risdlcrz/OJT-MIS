using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Settings bound from the "Mailtrap" configuration section.
    /// Prefer <c>Mailtrap__ApiToken</c> or the <c>MAILTRAP_API_TOKEN</c> environment variable
    /// over committing a token.
    /// </summary>
    public sealed class MailtrapOptions
    {
        public const string SectionName = "Mailtrap";

        public string ApiToken { get; set; } = string.Empty;

        /// <summary>Verified sender for the transactional API. Sandbox accepts the demo sender.</summary>
        public string SenderEmail { get; set; } = string.Empty;

        public string SenderName { get; set; } = "OJTMIS";

        /// <summary>Front-end base URL used by links inside the built-in templates.</summary>
        public string AppBaseUrl { get; set; } = "http://localhost:5173";

        /// <summary>
        /// Sandbox inbox id. When greater than zero, mail is posted to
        /// <c>https://sandbox.api.mailtrap.io/api/send/{inboxId}</c>.
        /// A sandbox token sent to the transactional host is what Mailtrap answers with HTTP 502.
        /// </summary>
        public long InboxId { get; set; }

        /// <summary>When true, and no inbox id is set, mail uses the bulk host instead of transactional.</summary>
        public bool UseBulkApi { get; set; }

        public string SendingBaseUrl { get; set; } = "https://send.api.mailtrap.io";

        public string BulkBaseUrl { get; set; } = "https://bulk.api.mailtrap.io";

        public string SandboxBaseUrl { get; set; } = "https://sandbox.api.mailtrap.io";
    }

    /// <summary>
    /// Sends account activation and password-reset mail through the Mailtrap Email API.
    /// The official client posts JSON to a host chosen by the token type:
    /// sandbox inbox, transactional <c>send.api.mailtrap.io</c>, or bulk.
    /// Missing credentials fall back to the local outbox in development.
    /// </summary>
    public sealed class MailtrapEmailService : IEmailService
    {
        public const string HttpClientName = "Mailtrap";

        /// <summary>Mailtrap rejects payloads above 10 MB on the sending API; keep headroom.</summary>
        private const long MaxAttachmentBytes = 10L * 1024 * 1024;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly MailtrapOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<MailtrapEmailService> _logger;

        public MailtrapEmailService(
            IOptions<MailtrapOptions> options,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            ILogger<MailtrapEmailService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SendAsync(EmailRequest message, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Validate(message);

            if (!IsConfigured)
            {
                return await WriteOutboxAsync(message, "Mailtrap credentials are not set.", cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(_options.SenderEmail))
            {
                throw new InvalidOperationException(
                    "Mailtrap is configured but Mailtrap:SenderEmail is empty. Set a sender address.");
            }

            var endpoint = BuildEndpoint();
            var payload = BuildPayload(message);
            var recipients = string.Join(", ", message.To);

            try
            {
                var messageId = await PostAsync(endpoint, payload, cancellationToken);
                _logger.LogInformation(
                    "Sent \"{Subject}\" to {Recipients} through Mailtrap at {Endpoint} ({MessageId}).",
                    message.Subject,
                    recipients,
                    endpoint,
                    messageId);
                return messageId;
            }
            catch (MailtrapDeliveryException ex) when (ex.StatusCode is 502 or 503 or 504 && _environment.IsDevelopment())
            {
                _logger.LogError(
                    ex,
                    "Mailtrap returned HTTP {Status} for \"{Subject}\" at {Endpoint}. Writing the message to the local outbox.",
                    ex.StatusCode,
                    message.Subject,
                    endpoint);
                return await WriteOutboxAsync(message, ex.Message, cancellationToken);
            }
            catch (HttpRequestException ex) when (_environment.IsDevelopment())
            {
                _logger.LogError(ex, "Mailtrap could not be reached for \"{Subject}\". Writing the message to the local outbox.", message.Subject);
                return await WriteOutboxAsync(message, ex.Message, cancellationToken);
            }
        }

        /// <summary>
        /// Simple one-recipient HTML email with an optional binary attachment.
        /// The bytes are base64-encoded in the Mailtrap payload; a .pdf file
        /// name gets the application/pdf MIME type automatically.
        /// </summary>
        public Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachment = null,
            string? attachmentName = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(to);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(htmlBody);

            var attachments = new List<EmailAttachment>();

            if (attachment is not null)
            {
                // PDF reports are the expected attachment, so fall back to a
                // .pdf name (and therefore the application/pdf MIME type).
                attachments.Add(new EmailAttachment
                {
                    FileName = string.IsNullOrWhiteSpace(attachmentName)
                        ? "attachment.pdf"
                        : attachmentName.Trim(),
                    Content = attachment
                });
            }

            return SendAsync(new EmailRequest
            {
                To = new List<string> { to.Trim() },
                Subject = subject.Trim(),
                HtmlBody = htmlBody,
                Attachments = attachments
            }, cancellationToken);
        }

        public Task SendTemporaryPasswordAsync(
            string email,
            string name,
            string temporaryPassword,
            CancellationToken cancellationToken = default)
        {
            var appBaseUrl = AppBaseUrl();
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
            var signIn = $"{appBaseUrl}/login.html";

            var textBody =
                $"{greeting},\r\n\r\n" +
                "Your OJT-MIS account is ready. Use the temporary password below to activate it and sign in.\r\n\r\n" +
                $"Sign in at {signIn}\r\n" +
                $"Temporary password: {temporaryPassword}\r\n\r\n" +
                "After you sign in, change this password from Account > Security.\r\n";

            return SendAsync(new EmailRequest
            {
                To = new List<string> { email },
                Subject = "Activate your OJT-MIS account",
                Category = "Account Activation",
                HtmlBody = EmailTemplates.TemporaryPassword(appBaseUrl, name, temporaryPassword),
                TextBody = textBody
            }, cancellationToken);
        }

        public Task SendPasswordResetLinkAsync(
            string email,
            string name,
            string token,
            CancellationToken cancellationToken = default)
        {
            var appBaseUrl = AppBaseUrl();
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
            var link = $"{appBaseUrl}/login.html?mode=reset&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            var textBody =
                $"{greeting},\r\n\r\n" +
                "A security request was made to reset the password for your OJT-MIS account.\r\n\r\n" +
                $"Reset your password: {link}\r\n\r\n" +
                "If you did not request this, you can ignore this message. The link expires with the reset token.\r\n";

            return SendAsync(new EmailRequest
            {
                To = new List<string> { email },
                Subject = "Reset your OJT-MIS password",
                Category = "Password Reset",
                HtmlBody = EmailTemplates.PasswordReset(name, link),
                TextBody = textBody
            }, cancellationToken);
        }

        private async Task<string> PostAsync(Uri endpoint, MailtrapSendPayload payload, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var token = ResolveApiToken()!;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.TryAddWithoutValidation("Api-Token", token);
                request.Content = JsonContent.Create(payload, mediaType: null, JsonOptions);

                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var status = (int)response.StatusCode;

                if (status is 502 or 503 or 504 && attempt == 1)
                {
                    _logger.LogWarning(
                        "Mailtrap returned HTTP {Status} from {Endpoint}. Retrying once. A sandbox token must use InboxId and the sandbox host; a sending token must use https://send.api.mailtrap.io/api/send.",
                        status,
                        endpoint);
                    await Task.Delay(TimeSpan.FromMilliseconds(400), cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new MailtrapDeliveryException(status, endpoint, Trim(body));
                }

                MailtrapSendResponse? parsed = null;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try
                    {
                        parsed = JsonSerializer.Deserialize<MailtrapSendResponse>(body, JsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException(
                            $"Mailtrap returned HTTP {status} from {endpoint} but the body was not JSON.",
                            ex);
                    }
                }

                if (parsed is { Success: false })
                {
                    var errors = parsed.Errors is { Count: > 0 }
                        ? string.Join(" ", parsed.Errors)
                        : "Mailtrap rejected the email.";
                    throw new InvalidOperationException($"Mailtrap rejected the email: {errors}");
                }

                return parsed?.MessageIds?.FirstOrDefault(id => !string.IsNullOrWhiteSpace(id))
                    ?? $"mailtrap:{Guid.NewGuid():N}";
            }

            throw new MailtrapDeliveryException(502, endpoint, "Mailtrap did not accept the email after a retry.");
        }

        private MailtrapSendPayload BuildPayload(EmailRequest message)
        {
            var payload = new MailtrapSendPayload
            {
                From = new MailtrapAddress(_options.SenderEmail.Trim(), NullIfBlank(_options.SenderName)),
                To = message.To.Select(address => new MailtrapAddress(address.Trim(), null)).ToList(),
                Subject = message.Subject,
                Text = message.TextBody,
                Html = message.HtmlBody,
                Category = NullIfBlank(message.Category),
                Headers = message.Headers is { Count: > 0 } ? message.Headers : null
            };

            if (message.Cc.Count > 0)
            {
                payload.Cc = message.Cc.Select(address => new MailtrapAddress(address.Trim(), null)).ToList();
            }

            if (message.Bcc.Count > 0)
            {
                payload.Bcc = message.Bcc.Select(address => new MailtrapAddress(address.Trim(), null)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(message.ReplyTo))
            {
                payload.ReplyTo = new MailtrapAddress(message.ReplyTo.Trim(), null);
            }

            if (message.Attachments.Count > 0)
            {
                payload.Attachments = message.Attachments.Select(attachment => new MailtrapAttachment
                {
                    Content = Convert.ToBase64String(attachment.Content),
                    Filename = attachment.FileName,
                    Type = attachment.ContentType ?? GetContentType(attachment.FileName),
                    Disposition = "attachment"
                }).ToList();
            }

            return payload;
        }

        private Uri BuildEndpoint()
        {
            var inboxId = ResolveInboxId();
            if (inboxId > 0)
            {
                var sandbox = MailtrapHost(_options.SandboxBaseUrl, "https://sandbox.api.mailtrap.io", "sandbox.api.mailtrap.io");
                return new Uri($"{sandbox}/api/send/{inboxId}");
            }

            if (_options.UseBulkApi)
            {
                var bulk = MailtrapHost(_options.BulkBaseUrl, "https://bulk.api.mailtrap.io", "bulk.api.mailtrap.io");
                return new Uri($"{bulk}/api/send");
            }

            var sending = MailtrapHost(_options.SendingBaseUrl, "https://send.api.mailtrap.io", "send.api.mailtrap.io");
            return new Uri($"{sending}/api/send");
        }

        private string MailtrapHost(string configured, string fallback, string expectedHost)
        {
            if (Uri.TryCreate(configured, UriKind.Absolute, out var uri) &&
                uri.Scheme == Uri.UriSchemeHttps &&
                uri.Host.Equals(expectedHost, StringComparison.OrdinalIgnoreCase))
            {
                return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
            }

            _logger.LogWarning(
                "Mailtrap base URL \"{Configured}\" is not https://{Host}. Using {Fallback} so the request is not sent to a gateway that returns HTTP 502.",
                configured,
                expectedHost,
                fallback);
            return fallback;
        }

        private string AppBaseUrl() =>
            string.IsNullOrWhiteSpace(_options.AppBaseUrl)
                ? "http://localhost:5173"
                : _options.AppBaseUrl.TrimEnd('/');

        private bool IsConfigured
        {
            get
            {
                var token = ResolveApiToken();
                return !string.IsNullOrWhiteSpace(token) && !IsPlaceholder(token);
            }
        }

        private string? ResolveApiToken()
        {
            if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            {
                return _options.ApiToken.Trim();
            }

            var fromEnvironment = Environment.GetEnvironmentVariable("MAILTRAP_API_TOKEN");
            return string.IsNullOrWhiteSpace(fromEnvironment) ? null : fromEnvironment.Trim();
        }

        private long ResolveInboxId()
        {
            if (_options.InboxId > 0) return _options.InboxId;

            var fromEnvironment = Environment.GetEnvironmentVariable("MAILTRAP_INBOX_ID");
            return long.TryParse(fromEnvironment, out var inboxId) && inboxId > 0 ? inboxId : 0;
        }

        private static bool IsPlaceholder(string token)
        {
            var value = token.Trim().Trim('<', '>');
            return value.Contains("your_api_token", StringComparison.OrdinalIgnoreCase)
                || value.Contains("your-api-token", StringComparison.OrdinalIgnoreCase)
                || value.Contains("your_mailtrap_api_token", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("re_your_api_key", StringComparison.OrdinalIgnoreCase)
                || value.Equals("change-me", StringComparison.OrdinalIgnoreCase);
        }

        private static void Validate(EmailRequest message)
        {
            if (message.To.Count == 0)
            {
                throw new ArgumentException("At least one recipient is required.", nameof(message));
            }

            foreach (var address in message.To.Concat(message.Cc).Concat(message.Bcc))
            {
                if (!MailAddress.TryCreate(address, out _))
                {
                    throw new ArgumentException($"'{address}' is not a valid email address.", nameof(message));
                }
            }

            if (!string.IsNullOrWhiteSpace(message.ReplyTo) && !MailAddress.TryCreate(message.ReplyTo, out _))
            {
                throw new ArgumentException($"'{message.ReplyTo}' is not a valid reply-to address.", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.Subject))
            {
                throw new ArgumentException("A subject is required.", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody))
            {
                throw new ArgumentException("An HTML or plain-text body is required.", nameof(message));
            }

            long totalBytes = 0;
            foreach (var attachment in message.Attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FileName) ||
                    Path.GetFileName(attachment.FileName) != attachment.FileName)
                {
                    throw new ArgumentException(
                        "Each attachment needs a plain file name without a directory.",
                        nameof(message));
                }

                if (attachment.Content.Length == 0)
                {
                    throw new ArgumentException($"Attachment '{attachment.FileName}' is empty.", nameof(message));
                }

                totalBytes += attachment.Content.LongLength;
            }

            if (totalBytes > MaxAttachmentBytes)
            {
                throw new ArgumentException(
                    $"Attachments total {totalBytes / (1024 * 1024)} MB; Mailtrap accepts at most 10 MB.",
                    nameof(message));
            }
        }

        private static string GetContentType(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".csv" => "text/csv",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string Trim(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "empty response";
            var compact = body.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
            return compact.Length <= 500 ? compact : compact[..500];
        }

        private async Task<string> WriteOutboxAsync(EmailRequest message, string reason, CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Mailtrap is not configured. Set Mailtrap:ApiToken (or the MAILTRAP_API_TOKEN environment variable). " +
                    "For the sandbox, also set Mailtrap:InboxId so mail is not posted to the transactional host.");
            }

            var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "outbox");
            Directory.CreateDirectory(folder);

            var baseName = $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            var html = message.HtmlBody ?? $"<pre>{WebUtility.HtmlEncode(message.TextBody)}</pre>";
            var file = Path.Combine(folder, baseName + ".html");

            var header =
                $"<!-- To: {WebUtility.HtmlEncode(string.Join(", ", message.To))} -->\r\n" +
                $"<!-- Subject: {WebUtility.HtmlEncode(message.Subject)} -->\r\n" +
                $"<!-- Reason: {WebUtility.HtmlEncode(reason)} -->\r\n";

            await File.WriteAllTextAsync(file, header + html, cancellationToken);

            foreach (var attachment in message.Attachments)
            {
                var attachmentPath = Path.Combine(folder, $"{baseName}-{attachment.FileName}");
                await File.WriteAllBytesAsync(attachmentPath, attachment.Content, cancellationToken);
            }

            _logger.LogWarning(
                "Mailtrap did not send \"{Subject}\" to {Recipients} ({Reason}). Wrote {File}.",
                message.Subject,
                string.Join(", ", message.To),
                reason,
                file);

            return $"outbox:{baseName}";
        }

        private sealed class MailtrapDeliveryException : InvalidOperationException
        {
            public MailtrapDeliveryException(int statusCode, Uri endpoint, string body)
                : base($"Mailtrap could not deliver the email (HTTP {statusCode} from {endpoint}). {body}")
            {
                StatusCode = statusCode;
            }

            public int StatusCode { get; }
        }

        private sealed class MailtrapSendPayload
        {
            [JsonPropertyName("from")]
            public MailtrapAddress From { get; set; } = new(string.Empty, null);

            [JsonPropertyName("to")]
            public List<MailtrapAddress> To { get; set; } = new();

            [JsonPropertyName("cc")]
            public List<MailtrapAddress>? Cc { get; set; }

            [JsonPropertyName("bcc")]
            public List<MailtrapAddress>? Bcc { get; set; }

            [JsonPropertyName("reply_to")]
            public MailtrapAddress? ReplyTo { get; set; }

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("html")]
            public string? Html { get; set; }

            [JsonPropertyName("category")]
            public string? Category { get; set; }

            [JsonPropertyName("attachments")]
            public List<MailtrapAttachment>? Attachments { get; set; }

            [JsonPropertyName("headers")]
            public Dictionary<string, string>? Headers { get; set; }
        }

        private sealed class MailtrapAddress
        {
            public MailtrapAddress(string email, string? name)
            {
                Email = email;
                Name = name;
            }

            [JsonPropertyName("email")]
            public string Email { get; }

            [JsonPropertyName("name")]
            public string? Name { get; }
        }

        private sealed class MailtrapAttachment
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            [JsonPropertyName("filename")]
            public string Filename { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = "application/octet-stream";

            [JsonPropertyName("disposition")]
            public string Disposition { get; set; } = "attachment";
        }

        private sealed class MailtrapSendResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; } = true;

            [JsonPropertyName("message_ids")]
            public List<string>? MessageIds { get; set; }

            [JsonPropertyName("errors")]
            public List<string>? Errors { get; set; }
        }
    }
}

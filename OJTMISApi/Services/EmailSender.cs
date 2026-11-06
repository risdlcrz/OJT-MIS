using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace OJTMISApi.Services
{
    public class EmailOptions
    {
        /// <summary>Auto, SendGrid, Graph, Gmail, or Smtp.</summary>
        public string Provider { get; set; } = "Auto";

        public string SendGridApiKey { get; set; } = string.Empty;

        public string GraphTenantId { get; set; } = string.Empty;
        public string GraphClientId { get; set; } = string.Empty;
        public string GraphClientSecret { get; set; } = string.Empty;
        /// <summary>Mailbox the Graph app is allowed to send as.</summary>
        public string GraphSender { get; set; } = string.Empty;

        public string GmailClientId { get; set; } = string.Empty;
        public string GmailClientSecret { get; set; } = string.Empty;
        public string GmailRefreshToken { get; set; } = string.Empty;

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "noreply@ojtmis.local";
        public string FromName { get; set; } = "OJTMIS";
        public bool EnableSsl { get; set; } = true;
        public string AppBaseUrl { get; set; } = "http://localhost:5173";
    }

    public interface IEmailSender
    {
        Task SendTemporaryPasswordAsync(string email, string name, string temporaryPassword);
        Task SendPasswordResetLinkAsync(string email, string name, string token);
    }

    /// <summary>
    /// Delivers mail through SendGrid, Microsoft Graph, or the Gmail API.
    /// SMTP is used when those are not configured. With no provider at all,
    /// the message is written to App_Data/outbox so local development still has a copy.
    /// </summary>
    public class ConfiguredEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ConfiguredEmailSender> _logger;

        public ConfiguredEmailSender(
            IOptions<EmailOptions> options,
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment environment,
            ILogger<ConfiguredEmailSender> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _environment = environment;
            _logger = logger;
        }

        public Task SendTemporaryPasswordAsync(string email, string name, string temporaryPassword)
        {
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
            var signIn = $"{AppBase()}/login.html";
            var body =
                $"{greeting},\r\n\r\n" +
                "An OJTMIS account was created for this email address.\r\n\r\n" +
                $"Sign in at {signIn}\r\n" +
                $"Temporary password: {temporaryPassword}\r\n\r\n" +
                "After you sign in, change this password from Account > Security.\r\n";

            return SendAsync(email, name, "Your OJTMIS temporary password", body);
        }

        public Task SendPasswordResetLinkAsync(string email, string name, string token)
        {
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
            var link = BuildResetLink(email, token);
            var body =
                $"{greeting},\r\n\r\n" +
                "A password reset was requested for your OJTMIS account.\r\n\r\n" +
                $"Reset your password: {link}\r\n\r\n" +
                "If you did not request this, you can ignore this message. The link expires with the reset token.\r\n";

            return SendAsync(email, name, "Reset your OJTMIS password", body);
        }

        private string AppBase() =>
            string.IsNullOrWhiteSpace(_options.AppBaseUrl)
                ? "http://localhost:5173"
                : _options.AppBaseUrl.TrimEnd('/');

        private string BuildResetLink(string email, string token) =>
            $"{AppBase()}/login.html?mode=reset&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

        private async Task SendAsync(string email, string name, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("A recipient email is required.");
            }

            var provider = ResolveProvider();
            switch (provider)
            {
                case "SendGrid":
                    await SendSendGridAsync(email, name, subject, body);
                    break;
                case "Graph":
                    await SendGraphAsync(email, name, subject, body);
                    break;
                case "Gmail":
                    await SendGmailAsync(email, name, subject, body);
                    break;
                case "Smtp":
                    await SendSmtpAsync(email, name, subject, body);
                    break;
                default:
                    await WriteOutboxAsync(email, subject, body);
                    return;
            }

            _logger.LogInformation("Sent {Subject} to {Email} via {Provider}.", subject, email, provider);
        }

        private string ResolveProvider()
        {
            var requested = (_options.Provider ?? "Auto").Trim();
            if (requested.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)) return Require("SendGrid", _options.SendGridApiKey, "Email:SendGridApiKey");
            if (requested.Equals("Graph", StringComparison.OrdinalIgnoreCase) || requested.Equals("MicrosoftGraph", StringComparison.OrdinalIgnoreCase))
            {
                Require("Graph", _options.GraphTenantId, "Email:GraphTenantId");
                Require("Graph", _options.GraphClientId, "Email:GraphClientId");
                Require("Graph", _options.GraphClientSecret, "Email:GraphClientSecret");
                return "Graph";
            }
            if (requested.Equals("Gmail", StringComparison.OrdinalIgnoreCase) || requested.Equals("Google", StringComparison.OrdinalIgnoreCase))
            {
                Require("Gmail", _options.GmailClientId, "Email:GmailClientId");
                Require("Gmail", _options.GmailClientSecret, "Email:GmailClientSecret");
                Require("Gmail", _options.GmailRefreshToken, "Email:GmailRefreshToken");
                return "Gmail";
            }
            if (requested.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            {
                return Require("Smtp", _options.Host, "Email:Host");
            }

            if (!string.IsNullOrWhiteSpace(_options.SendGridApiKey)) return "SendGrid";
            if (HasGraph()) return "Graph";
            if (!string.IsNullOrWhiteSpace(_options.GmailRefreshToken) &&
                !string.IsNullOrWhiteSpace(_options.GmailClientId) &&
                !string.IsNullOrWhiteSpace(_options.GmailClientSecret))
            {
                return "Gmail";
            }
            if (!string.IsNullOrWhiteSpace(_options.Host)) return "Smtp";
            return "Outbox";
        }

        private static string Require(string provider, string value, string setting)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"{provider} is selected but {setting} is empty.");
            }

            return provider == "Smtp" ? "Smtp" : provider;
        }

        private bool HasGraph() =>
            !string.IsNullOrWhiteSpace(_options.GraphTenantId) &&
            !string.IsNullOrWhiteSpace(_options.GraphClientId) &&
            !string.IsNullOrWhiteSpace(_options.GraphClientSecret);

        private async Task SendSendGridAsync(string email, string name, string subject, string body)
        {
            var payload = new
            {
                personalizations = new[]
                {
                    new { to = new[] { new { email, name = name ?? string.Empty } } }
                },
                from = new { email = _options.FromAddress, name = _options.FromName },
                subject,
                content = new[]
                {
                    new { type = "text/plain", value = body },
                    new { type = "text/html", value = ToHtml(body) }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SendGridApiKey.Trim());
            request.Content = JsonContent(payload);
            await SendHttpAsync(request, "SendGrid");
        }

        private async Task SendGraphAsync(string email, string name, string subject, string body)
        {
            var token = await RequestTokenAsync(
                $"https://login.microsoftonline.com/{_options.GraphTenantId.Trim()}/oauth2/v2.0/token",
                new Dictionary<string, string>
                {
                    ["client_id"] = _options.GraphClientId.Trim(),
                    ["client_secret"] = _options.GraphClientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                },
                "Microsoft Graph");

            var sender = string.IsNullOrWhiteSpace(_options.GraphSender)
                ? _options.FromAddress
                : _options.GraphSender.Trim();

            var payload = new
            {
                message = new
                {
                    subject,
                    body = new { contentType = "Text", content = body },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { address = email, name = name ?? string.Empty } }
                    }
                },
                saveToSentItems = false
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(sender)}/sendMail");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent(payload);
            await SendHttpAsync(request, "Microsoft Graph");
        }

        private async Task SendGmailAsync(string email, string name, string subject, string body)
        {
            var token = await RequestTokenAsync(
                "https://oauth2.googleapis.com/token",
                new Dictionary<string, string>
                {
                    ["client_id"] = _options.GmailClientId.Trim(),
                    ["client_secret"] = _options.GmailClientSecret,
                    ["refresh_token"] = _options.GmailRefreshToken.Trim(),
                    ["grant_type"] = "refresh_token"
                },
                "Gmail");

            var from = string.IsNullOrWhiteSpace(_options.FromAddress)
                ? "me"
                : _options.FromAddress.Trim();
            var raw = new StringBuilder()
                .Append("From: ").Append(EncodeHeader(_options.FromName, from)).Append("\r\n")
                .Append("To: ").Append(EncodeHeader(name, email)).Append("\r\n")
                .Append("Subject: ").Append(EncodeSubject(subject)).Append("\r\n")
                .Append("MIME-Version: 1.0\r\n")
                .Append("Content-Type: text/plain; charset=utf-8\r\n")
                .Append("\r\n")
                .Append(body)
                .ToString();

            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://gmail.googleapis.com/gmail/v1/users/me/messages/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent(new { raw = encoded });
            await SendHttpAsync(request, "Gmail");
        }

        private async Task<string> RequestTokenAsync(string url, Dictionary<string, string> form, string provider)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(form)
            };
            var response = await SendRawAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"{provider} rejected the credentials ({(int)response.StatusCode}). {Trim(json)}");
            }

            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("access_token", out var token) || token.GetString() is not { Length: > 0 } value)
            {
                throw new InvalidOperationException($"{provider} did not return an access token.");
            }

            return value;
        }

        private async Task SendSmtpAsync(string email, string name, string subject, string body)
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(email, name ?? string.Empty));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.User))
            {
                client.Credentials = new NetworkCredential(_options.User, _options.Password);
            }

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"SMTP could not deliver the message: {ex.Message}", ex);
            }
        }

        private async Task SendHttpAsync(HttpRequestMessage request, string provider)
        {
            var response = await SendRawAsync(request);
            if (response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"{provider} could not deliver the message ({(int)response.StatusCode}). {Trim(json)}");
        }

        private async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request)
        {
            var client = _httpClientFactory.CreateClient("ojtmis-email");
            var response = await client.SendAsync(await CopyAsync(request));
            if ((int)response.StatusCode is not (429 or >= 500))
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(600);
            return await client.SendAsync(await CopyAsync(request));
        }

        private static async Task<HttpRequestMessage> CopyAsync(HttpRequestMessage request)
        {
            var copy = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is null) return copy;

            var mediaType = request.Content.Headers.ContentType;
            var bytes = await request.Content.ReadAsByteArrayAsync();
            request.Content = new ByteArrayContent(bytes);
            copy.Content = new ByteArrayContent(bytes);
            if (mediaType is not null)
            {
                request.Content.Headers.ContentType = mediaType;
                copy.Content.Headers.ContentType = mediaType;
            }

            return copy;
        }

        private static StringContent JsonContent(object payload) =>
            new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        private static string ToHtml(string body) =>
            "<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:15px;line-height:1.5;color:#1f2937\">" +
            string.Join("<br>", body.Replace("\r\n", "\n").Split('\n').Select(WebUtility.HtmlEncode)) +
            "</div>";

        private static string EncodeHeader(string? name, string email)
        {
            if (string.IsNullOrWhiteSpace(name)) return email;
            return $"\"{name.Replace("\"", "")}\" <{email}>";
        }

        private static string EncodeSubject(string subject)
        {
            if (subject.All(c => c < 128)) return subject;
            return "=?UTF-8?B?" + Convert.ToBase64String(Encoding.UTF8.GetBytes(subject)) + "?=";
        }

        private static string Trim(string value)
        {
            var text = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length <= 400 ? text : text[..400];
        }

        private async Task WriteOutboxAsync(string email, string subject, string body)
        {
            var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "outbox");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
            var contents =
                $"To: {email}\r\nSubject: {subject}\r\n\r\n{body}\r\n\r\n" +
                "No mail provider is configured. Set Email:SendGridApiKey, Email:Graph*, Email:Gmail*, or Email:Host.\r\n";
            await File.WriteAllTextAsync(file, contents);
            _logger.LogWarning("No mail provider is configured. Wrote {Subject} for {Email} to {File}.", subject, email, file);
        }
    }
}

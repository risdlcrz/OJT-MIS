using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace OJTMISApi.Services
{
    public class EmailOptions
    {
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
    /// Sends mail through SMTP. When Email:Host is empty, messages are written
    /// to App_Data/outbox so local development still delivers them.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailOptions> options,
            IWebHostEnvironment environment,
            ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _environment = environment;
            _logger = logger;
        }

        public Task SendTemporaryPasswordAsync(string email, string name, string temporaryPassword)
        {
            var greeting = string.IsNullOrWhiteSpace(name) ? "Hello" : $"Hello {name}";
            var body =
                $"{greeting},\r\n\r\n" +
                "An OJTMIS account was created for this email address.\r\n\r\n" +
                $"Sign in at {_options.AppBaseUrl.TrimEnd('/')}/login.html\r\n" +
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
                "If you did not request this, you can ignore this message.\r\n";

            return SendAsync(email, name, "Reset your OJTMIS password", body);
        }

        private string BuildResetLink(string email, string token)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.AppBaseUrl)
                ? "http://localhost:5173"
                : _options.AppBaseUrl.TrimEnd('/');

            return $"{baseUrl}/login.html?mode=reset&email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        }

        private async Task SendAsync(string email, string name, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("A recipient email is required.");
            }

            if (string.IsNullOrWhiteSpace(_options.Host))
            {
                await WriteOutboxAsync(email, subject, body);
                return;
            }

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

            await client.SendMailAsync(message);
            _logger.LogInformation("Sent {Subject} to {Email}.", subject, email);
        }

        private async Task WriteOutboxAsync(string email, string subject, string body)
        {
            var folder = Path.Combine(_environment.ContentRootPath, "App_Data", "outbox");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{DateTime.Now:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
            var contents = $"To: {email}\r\nSubject: {subject}\r\n\r\n{body}";
            await File.WriteAllTextAsync(file, contents);
            _logger.LogInformation("Email host is not configured. Wrote {Subject} for {Email} to {File}.", subject, email, file);
        }
    }
}

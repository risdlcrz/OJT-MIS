using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Per-IP limits for the anonymous auth endpoints, on top of Identity's
    /// per-account lockout (5 wrong passwords = 5 minute lock).
    /// </summary>
    public static class RateLimits
    {
        /// <summary>login, confirm-email, reset-password: 10 attempts per minute per IP.</summary>
        public const string SignIn = "auth-signin";

        /// <summary>register, forgot-password, resend-confirmation: 5 per 15 minutes per IP (each sends mail).</summary>
        public const string AccountEmail = "auth-email";

        public static IServiceCollection AddAuthRateLimits(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(SignIn, http => PerIp(http, permits: 10, window: TimeSpan.FromMinutes(1)));
                options.AddPolicy(AccountEmail, http => PerIp(http, permits: 5, window: TimeSpan.FromMinutes(15)));

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var wait = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? retryAfter
                        : TimeSpan.FromMinutes(1);
                    var seconds = Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));

                    context.HttpContext.Response.Headers.RetryAfter = seconds.ToString();
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        message = seconds >= 120
                            ? $"Too many attempts. Try again in {(int)Math.Ceiling(seconds / 60.0)} minutes."
                            : $"Too many attempts. Try again in {seconds} seconds."
                    }, cancellationToken);
                };
            });
        }

        // Behind a reverse proxy, configure ForwardedHeaders so this is the
        // client's address rather than the proxy's.
        private static RateLimitPartition<string> PerIp(HttpContext http, int permits, TimeSpan window) =>
            RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = window,
                    QueueLimit = 0
                });
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Turns any unhandled exception into a JSON ProblemDetails reply with a
    /// short <c>message</c> field (which the Vue client shows), instead of an
    /// empty 500 or a stack trace. The exception, its SQL error and stack
    /// trace are written only to the server log, in every environment; the
    /// reply carries a reference id that matches that log entry.
    /// </summary>
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problems;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            IProblemDetailsService problems,
            ILogger<GlobalExceptionHandler> logger)
        {
            _problems = problems;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken cancellationToken)
        {
            // The browser gave up (navigation, closed tab): nothing to report.
            if (exception is OperationCanceledException && http.RequestAborted.IsCancellationRequested)
            {
                http.Response.StatusCode = 499;
                return true;
            }

            var reference = Activity.Current?.Id ?? http.TraceIdentifier;

            var (status, title, message) = exception switch
            {
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                    "This record was changed by someone else. Reload the page and try again."),
                DbUpdateException => (StatusCodes.Status409Conflict, "Could not save",
                    "The change could not be saved because it conflicts with existing data. Check for duplicates and try again."),
                BadHttpRequestException bad => (bad.StatusCode, "Bad request",
                    "The request could not be read. Check the form and try again."),
                _ => (StatusCodes.Status500InternalServerError, "Server error", SafeErrors.Server)
            };

            if (status >= 500)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path} (reference {Reference}).",
                    http.Request.Method, http.Request.Path, reference);
            }
            else
            {
                _logger.LogWarning(exception, "Request failed with {Status} for {Method} {Path} (reference {Reference}).",
                    status, http.Request.Method, http.Request.Path, reference);
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Never the exception text: it can hold SQL, file paths and
                // stack frames. Developers read the log, matched by reference.
                Detail = null,
                Instance = http.Request.Path
            };
            problem.Extensions["message"] = message;
            problem.Extensions["reference"] = reference;

            http.Response.StatusCode = status;
            return await _problems.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = http,
                ProblemDetails = problem,
                Exception = exception
            });
        }
    }
}

using System.Threading.Channels;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Sends account emails in the background for the public auth endpoints
    /// (forgot password, sign-up, resend verification). Those endpoints answer
    /// the same way whether or not the email has an account; sending the mail
    /// inline would make the "has an account" reply slower and give it away.
    /// </summary>
    public sealed class EmailQueue
    {
        private readonly Channel<(string Description, Func<IEmailService, Task> Send)> _channel =
            Channel.CreateBounded<(string, Func<IEmailService, Task>)>(new BoundedChannelOptions(500)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });

        public ChannelReader<(string Description, Func<IEmailService, Task> Send)> Reader => _channel.Reader;

        /// <summary>Queues an email. Returns false if the queue is full (the mail is dropped).</summary>
        public bool Enqueue(string description, Func<IEmailService, Task> send) =>
            _channel.Writer.TryWrite((description, send));
    }

    /// <summary>Drains <see cref="EmailQueue"/> one message at a time.</summary>
    public sealed class EmailQueueWorker : BackgroundService
    {
        private readonly EmailQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<EmailQueueWorker> _logger;

        public EmailQueueWorker(EmailQueue queue, IServiceScopeFactory scopes, ILogger<EmailQueueWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var (description, send) in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    await send(scope.ServiceProvider.GetRequiredService<IEmailService>());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not send queued email: {Description}.", description);
                }
            }
        }
    }
}

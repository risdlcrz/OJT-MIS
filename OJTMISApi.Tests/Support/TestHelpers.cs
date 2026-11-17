using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;

// Several tests move the shared AppClock, so tests must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OJTMISApi.Tests.Support
{
    /// <summary>Freezes <see cref="AppClock"/> at an office-local time until disposed.</summary>
    public sealed class FrozenClock : IDisposable
    {
        private readonly Func<DateTime> _previous;
        private DateTime _utc;

        public FrozenClock(DateTime officeLocal)
        {
            _previous = AppClock.UtcNowSource;
            Set(officeLocal);
            AppClock.UtcNowSource = () => _utc;
        }

        public void Set(DateTime officeLocal) => _utc = AppClock.ToUtc(officeLocal);

        public void Advance(TimeSpan by) => _utc = _utc.Add(by);

        public void Dispose() => AppClock.UtcNowSource = _previous;
    }

    public static class TestDb
    {
        /// <summary>A fresh, isolated in-memory database.</summary>
        public static ApplicationDbContext Create() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);

        /// <summary>A signed-in principal carrying just an email claim.</summary>
        public static ClaimsPrincipal UserWithEmail(string email) =>
            new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }, "Test"));
    }
}

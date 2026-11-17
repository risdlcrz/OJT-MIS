using OJTMISApi.Tests.Support;

namespace OJTMISApi.Tests
{
    public class AppClockTests
    {
        [Fact]
        public void Now_is_Philippine_time_regardless_of_server_zone()
        {
            var previous = AppClock.UtcNowSource;
            try
            {
                AppClock.UtcNowSource = () => new DateTime(2026, 3, 1, 23, 30, 0, DateTimeKind.Utc);

                Assert.Equal(new DateTime(2026, 3, 2, 7, 30, 0), AppClock.Now);
                Assert.Equal(new DateTime(2026, 3, 2), AppClock.Today);
                Assert.Equal(DateTimeKind.Unspecified, AppClock.Now.Kind);
            }
            finally
            {
                AppClock.UtcNowSource = previous;
            }
        }

        [Fact]
        public void ToUtc_subtracts_eight_hours()
        {
            Assert.Equal(
                new DateTime(2025, 12, 31, 16, 0, 0),
                AppClock.ToUtc(new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void FrozenClock_round_trips_office_time()
        {
            using var clock = new FrozenClock(new DateTime(2026, 5, 4, 8, 15, 0));
            Assert.Equal(new DateTime(2026, 5, 4, 8, 15, 0), AppClock.Now);

            clock.Advance(TimeSpan.FromMinutes(10));
            Assert.Equal(new DateTime(2026, 5, 4, 8, 25, 0), AppClock.Now);
        }
    }
}

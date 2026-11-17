namespace OJTMISApi
{
    /// <summary>
    /// The one place the application reads the current time.
    ///
    /// All stored dates and times (attendance punches, DTRs, logs, OJT
    /// periods) are Philippine local time. Reading <c>DateTime.Now</c> would
    /// instead use the server's own time zone, which on a cloud host is
    /// usually UTC - shifting every punch by 8 hours and moving the
    /// "today" boundary. This clock always answers in the configured office
    /// time zone (App:TimeZone, default Asia/Manila), whatever the server is
    /// set to.
    /// </summary>
    public static class AppClock
    {
        public const string DefaultTimeZoneId = "Asia/Manila";

        private static TimeZoneInfo _zone = Resolve(DefaultTimeZoneId);

        /// <summary>The office time zone used for every stored date and time.</summary>
        public static TimeZoneInfo Zone => _zone;

        /// <summary>Source of the current UTC time. Tests replace it to freeze or move time.</summary>
        internal static Func<DateTime> UtcNowSource { get; set; } = () => DateTime.UtcNow;

        /// <summary>Current office-local date and time (Kind = Unspecified, as stored).</summary>
        public static DateTime Now =>
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(UtcNowSource(), _zone), DateTimeKind.Unspecified);

        /// <summary>Current office-local date.</summary>
        public static DateTime Today => Now.Date;

        /// <summary>Converts an office-local date/time to UTC (for token expiry and the like).</summary>
        public static DateTime ToUtc(DateTime officeLocal) =>
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(officeLocal, DateTimeKind.Unspecified), _zone);

        /// <summary>Called once at startup with App:TimeZone.</summary>
        public static void Configure(string? timeZoneId)
        {
            _zone = Resolve(string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId.Trim());
        }

        private static TimeZoneInfo Resolve(string id)
        {
            try
            {
                // IANA ids ("Asia/Manila") work on Windows 10+ and Linux in .NET 6+.
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception) when (id == DefaultTimeZoneId)
            {
                // The Philippines has no daylight saving time: a fixed UTC+8 is exact.
                return TimeZoneInfo.CreateCustomTimeZone(id, TimeSpan.FromHours(8), "Philippine Time", "Philippine Time");
            }
        }
    }
}

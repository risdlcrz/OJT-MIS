namespace OJTMISApi.Models
{
    /// <summary>
    /// Daily punches run in order: AM in, AM out, PM in, PM out.
    /// Late applies only when AM time in is after 8:00 AM. PM time in does not change that status.
    /// </summary>
    public static class AttendanceShift
    {
        public const string Morning = "Morning";
        public const string Afternoon = "Afternoon";
        public const string OnTime = "On Time";
        public const string Late = "Late";

        public static readonly TimeSpan MorningStart = new(8, 0, 0);

        public static string MorningStatus(DateTime timeIn) =>
            timeIn.TimeOfDay > MorningStart ? Late : OnTime;

        public static string TimeInStatus(string shift, DateTime timeIn) =>
            string.Equals(shift, Morning, StringComparison.OrdinalIgnoreCase)
                ? MorningStatus(timeIn)
                : OnTime;

        public static double Hours(DateTime timeIn, DateTime? timeOut) =>
            timeOut is null ? 0 : Math.Max(0, (timeOut.Value - timeIn).TotalHours);

        public static double Hours(DailyTimeRecord? record) =>
            record is null ? 0 : Hours(record.TimeIn, record.TimeOut);

        public static object? ShapeDay(DateTime date, IReadOnlyList<DailyTimeRecord> records)
        {
            if (records.Count == 0) return null;

            var morning = Latest(records, Morning);
            var afternoon = Latest(records, Afternoon);
            var hours = Hours(morning) + Hours(afternoon);
            var late = morning is not null && morning.TimeIn.TimeOfDay > MorningStart;

            return new
            {
                date,
                morning = View(morning),
                afternoon = View(afternoon),
                hours = Math.Round(hours, 2),
                status = late ? Late : OnTime,
                clockedIn = (morning is not null && morning.TimeOut is null)
                    || (afternoon is not null && afternoon.TimeOut is null)
            };
        }

        private static DailyTimeRecord? Latest(IReadOnlyList<DailyTimeRecord> records, string shift) =>
            records
                .Where(x => string.Equals(Normalize(x), shift, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.TimeIn)
                .FirstOrDefault();

        private static string Normalize(DailyTimeRecord record) =>
            string.Equals(record.ShiftType, Afternoon, StringComparison.OrdinalIgnoreCase)
                ? Afternoon
                : Morning;

        private static object? View(DailyTimeRecord? record) => record is null
            ? null
            : new
            {
                record.Id,
                record.TimeIn,
                record.TimeOut,
                record.Status,
                record.ShiftType
            };
    }
}

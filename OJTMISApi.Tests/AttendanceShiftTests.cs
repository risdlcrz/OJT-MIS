using OJTMISApi.Models;

namespace OJTMISApi.Tests
{
    public class AttendanceShiftTests
    {
        private static readonly DateTime Day = new(2026, 6, 15);

        [Theory]
        [InlineData(7, 30, "On Time")]
        [InlineData(8, 0, "On Time")]
        [InlineData(8, 1, "Late")]
        [InlineData(13, 0, "Late")]
        public void Morning_time_in_after_8am_is_late(int hour, int minute, string expected)
        {
            Assert.Equal(expected, AttendanceShift.MorningStatus(Day.AddHours(hour).AddMinutes(minute)));
        }

        [Fact]
        public void Afternoon_time_in_is_never_late_by_itself()
        {
            Assert.Equal(AttendanceShift.OnTime,
                AttendanceShift.TimeInStatus(AttendanceShift.Afternoon, Day.AddHours(15)));
        }

        [Fact]
        public void Hours_are_zero_without_time_out_and_never_negative()
        {
            Assert.Equal(0, AttendanceShift.Hours(Day.AddHours(8), null));
            Assert.Equal(0, AttendanceShift.Hours(Day.AddHours(9), Day.AddHours(8)));
            Assert.Equal(4, AttendanceShift.Hours(Day.AddHours(8), Day.AddHours(12)), 3);
        }

        [Fact]
        public void ShapeDay_sums_both_shifts_and_flags_open_punches()
        {
            var records = new List<DailyTimeRecord>
            {
                new() { Date = Day, ShiftType = AttendanceShift.Morning, TimeIn = Day.AddHours(8).AddMinutes(30), TimeOut = Day.AddHours(12) },
                new() { Date = Day, ShiftType = AttendanceShift.Afternoon, TimeIn = Day.AddHours(13) }
            };

            dynamic shaped = AttendanceShift.ShapeDay(Day, records)!;

            Assert.Equal(3.5, (double)shaped.hours, 2);
            Assert.Equal(AttendanceShift.Late, (string)shaped.status);
            Assert.True((bool)shaped.clockedIn);
        }

        [Fact]
        public void ShapeDay_returns_null_for_an_empty_day()
        {
            Assert.Null(AttendanceShift.ShapeDay(Day, Array.Empty<DailyTimeRecord>()));
        }
    }
}

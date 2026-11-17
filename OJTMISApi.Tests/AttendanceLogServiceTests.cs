using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;
using OJTMISApi.Tests.Support;

namespace OJTMISApi.Tests
{
    /// <summary>The QR punch flow an intern goes through every day.</summary>
    public class AttendanceLogServiceTests : IDisposable
    {
        private const string Email = "intern@example.test";
        private static readonly DateTime Day = new(2026, 6, 15);

        private readonly ApplicationDbContext _db = TestDb.Create();
        private readonly AttendanceQrService _qr = new();
        private readonly FrozenClock _clock = new(Day.AddHours(7).AddMinutes(55));
        private readonly AttendanceLogService _service;
        private readonly Intern _intern;

        public AttendanceLogServiceTests()
        {
            _intern = new Intern
            {
                FullName = "Juan Dela Cruz",
                School = "Test University",
                Email = Email,
                Status = "Hired",
                AttendanceQrToken = _qr.NewToken()
            };
            _db.Interns.Add(_intern);
            _db.SaveChanges();

            // The email claim is enough to find the intern, so no UserManager is needed.
            _service = new AttendanceLogService(_db, null!, _qr, new InternCompletionService(_db, null!));
        }

        public void Dispose()
        {
            _clock.Dispose();
            _db.Dispose();
        }

        private Task<(int Status, object Body)> Scan(string? payload = null) =>
            _service.LogAsync(TestDb.UserWithEmail(Email), payload ?? _intern.AttendanceQrToken);

        private static string Message(object body) =>
            (string)body.GetType().GetProperty("message")!.GetValue(body)!;

        [Fact]
        public async Task A_full_day_records_four_punches_in_order()
        {
            var punches = new List<string>();
            foreach (var at in new[] { 7.92, 12.0, 13.0, 17.0 })
            {
                _clock.Set(Day.AddHours(at));
                var (status, body) = await Scan();
                Assert.Equal(StatusCodes.Status200OK, status);
                punches.Add((string)body.GetType().GetProperty("punch")!.GetValue(body)!);
            }

            Assert.Equal(new[] { "AM Time In", "AM Time Out", "PM Time In", "PM Time Out" }, punches);

            _clock.Set(Day.AddHours(18));
            var (fifth, _) = await Scan();
            Assert.Equal(StatusCodes.Status409Conflict, fifth);

            var records = await _db.DailyTimeRecords.AsNoTracking().ToListAsync();
            Assert.Equal(2, records.Count);
            Assert.All(records, r => Assert.Equal(AttendanceShift.OnTime, r.Status));
        }

        [Fact]
        public async Task Arriving_after_8am_is_marked_late()
        {
            _clock.Set(Day.AddHours(8).AddMinutes(20));
            await Scan();

            var record = await _db.DailyTimeRecords.SingleAsync();
            Assert.Equal(AttendanceShift.Late, record.Status);
        }

        [Fact]
        public async Task A_double_scan_within_five_minutes_is_refused()
        {
            await Scan();

            _clock.Advance(TimeSpan.FromMinutes(2));
            var (status, body) = await Scan();

            Assert.Equal(StatusCodes.Status429TooManyRequests, status);
            Assert.Contains("Wait 3 more minute", Message(body));
            Assert.Null((await _db.DailyTimeRecords.SingleAsync()).TimeOut);

            _clock.Advance(TimeSpan.FromMinutes(3));
            var (later, _) = await Scan();
            Assert.Equal(StatusCodes.Status200OK, later);
        }

        [Fact]
        public async Task Another_interns_code_is_rejected()
        {
            var (status, _) = await Scan(_qr.NewToken());

            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Empty(_db.DailyTimeRecords);
        }

        [Fact]
        public async Task A_replaced_ID_card_stops_working()
        {
            var oldCard = _intern.AttendanceQrToken;
            _intern.AttendanceQrToken = _qr.NewToken();
            await _db.SaveChangesAsync();

            var (status, _) = await Scan(oldCard);
            Assert.Equal(StatusCodes.Status400BadRequest, status);

            var (legacyStatus, legacyBody) = await Scan("OJT1.1.signature");
            Assert.Equal(StatusCodes.Status400BadRequest, legacyStatus);
            Assert.Contains("replaced", Message(legacyBody));
        }

        [Fact]
        public async Task Interns_without_a_generated_ID_cannot_punch()
        {
            _intern.AttendanceQrToken = null;
            await _db.SaveChangesAsync();

            var (status, body) = await Scan("OJT2.anything");

            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Contains("No ID has been issued", Message(body));
        }
    }
}

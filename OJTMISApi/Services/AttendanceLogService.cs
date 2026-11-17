using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    public class AttendanceLogService
    {
        /// <summary>Shortest allowed time between two punches of the same intern.</summary>
        public static readonly TimeSpan MinimumPunchGap = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AttendanceQrService _qr;
        private readonly InternCompletionService _completion;

        public AttendanceLogService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AttendanceQrService qr,
            InternCompletionService completion)
        {
            _context = context;
            _userManager = userManager;
            _qr = qr;
            _completion = completion;
        }

        public async Task<(int Status, object Body)> LogAsync(ClaimsPrincipal user, string? payload)
        {
            var intern = await ResolveInternAsync(user);
            if (intern is null)
            {
                return (StatusCodes.Status403Forbidden, new
                {
                    message = "This account is not linked to a hired intern record."
                });
            }

            if (string.IsNullOrEmpty(intern.AttendanceQrToken))
            {
                return (StatusCodes.Status400BadRequest, new
                {
                    message = "No ID has been issued to you yet. Ask HR to generate your intern ID."
                });
            }

            if (!_qr.Matches(payload, intern.AttendanceQrToken))
            {
                return (StatusCodes.Status400BadRequest, new
                {
                    message = AttendanceQrService.IsLegacy(payload)
                        ? "This ID was replaced. Scan the QR code on your newest intern ID, or ask HR to generate one."
                        : "This QR code is not valid for your intern record. Scan the QR code on your own, latest intern ID."
                });
            }

            var now = AppClock.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var dayRecords = await _context.DailyTimeRecords
                .Where(x => x.InternId == intern.Id && x.Date >= today && x.Date < tomorrow)
                .OrderBy(x => x.TimeIn)
                .ToListAsync();

            // Guard against a double scan turning "Time In" into an instant
            // "Time Out" (or skipping a whole shift).
            var lastPunch = dayRecords
                .SelectMany(x => new[] { (DateTime?)x.TimeIn, x.TimeOut })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
            var sinceLast = now - lastPunch;
            if (sinceLast >= TimeSpan.Zero && sinceLast < MinimumPunchGap)
            {
                var wait = Math.Max(1, (int)Math.Ceiling((MinimumPunchGap - sinceLast).TotalMinutes));
                return (StatusCodes.Status429TooManyRequests, new
                {
                    message = $"Your last punch was recorded at {lastPunch:h:mm tt}. Wait {wait} more minute(s) before scanning again."
                });
            }

            var morning = dayRecords.FirstOrDefault(x =>
                !string.Equals(x.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase));
            var afternoon = dayRecords.FirstOrDefault(x =>
                string.Equals(x.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase));

            string action;
            string punch;
            DailyTimeRecord record;

            if (morning is null)
            {
                record = new DailyTimeRecord
                {
                    InternId = intern.Id,
                    InternName = intern.FullName,
                    InternEmail = intern.Email,
                    Date = today,
                    ShiftType = AttendanceShift.Morning,
                    TimeIn = now,
                    Status = AttendanceShift.MorningStatus(now),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.DailyTimeRecords.Add(record);
                action = "in";
                punch = "AM Time In";
            }
            else if (morning.TimeOut is null)
            {
                morning.TimeOut = now;
                morning.UpdatedAt = now;
                record = morning;
                action = "out";
                punch = "AM Time Out";
            }
            else if (afternoon is null)
            {
                record = new DailyTimeRecord
                {
                    InternId = intern.Id,
                    InternName = intern.FullName,
                    InternEmail = intern.Email,
                    Date = today,
                    ShiftType = AttendanceShift.Afternoon,
                    TimeIn = now,
                    Status = morning.Status,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.DailyTimeRecords.Add(record);
                action = "in";
                punch = "PM Time In";
            }
            else if (afternoon.TimeOut is null)
            {
                afternoon.TimeOut = now;
                afternoon.Status = morning.Status;
                afternoon.UpdatedAt = now;
                record = afternoon;
                action = "out";
                punch = "PM Time Out";
            }
            else
            {
                return (StatusCodes.Status409Conflict, new
                {
                    message = "All shift logs completed for today."
                });
            }

            await _context.SaveChangesAsync();

            var rendered = await _completion.RenderedHoursAsync(intern.Id);

            // When the required hours are reached, mark the intern
            // Completed and send HR a one-time notification.
            await _completion.CheckAsync(intern.Id, rendered);

            var saved = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == intern.Id && x.Date >= today && x.Date < tomorrow)
                .ToListAsync();
            var day = AttendanceShift.ShapeDay(today, saved);
            return (StatusCodes.Status200OK, new
            {
                action,
                punch,
                shift = punch,
                record,
                today = day,
                hoursRendered = Math.Round(rendered, 2),
                remainingHours = Math.Round(Math.Max(0, intern.RequiredHours - rendered), 2),
                clockedIn = saved.Any(x => x.TimeOut is null)
            });
        }

        public async Task<Intern?> ResolveInternAsync(ClaimsPrincipal user)
        {
            var email = user.FindFirstValue(ClaimTypes.Email)
                ?? (await _userManager.GetUserAsync(user))?.Email;
            if (string.IsNullOrWhiteSpace(email)) return null;

            var key = email.Trim().ToLower();
            return await _context.Interns
                .Where(i => i.Email != null && i.Email.ToLower() == key && i.Status == "Hired")
                .OrderByDescending(i => i.EndDate)
                .FirstOrDefaultAsync();
        }
    }
}

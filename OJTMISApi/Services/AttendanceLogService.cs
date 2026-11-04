using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    public class AttendanceLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AttendanceQrService _qr;

        public AttendanceLogService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            AttendanceQrService qr)
        {
            _context = context;
            _userManager = userManager;
            _qr = qr;
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

            if (!_qr.TryRead(payload, out var internId) || internId != intern.Id)
            {
                return (StatusCodes.Status400BadRequest, new
                {
                    message = "This QR code is not valid for your intern record."
                });
            }

            var now = DateTime.Now;
            var today = now.Date;
            var tomorrow = today.AddDays(1);
            var dayRecords = await _context.DailyTimeRecords
                .Where(x => x.InternId == intern.Id && x.Date >= today && x.Date < tomorrow)
                .OrderBy(x => x.TimeIn)
                .ToListAsync();

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

            var saved = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == intern.Id && x.Date >= today && x.Date < tomorrow)
                .ToListAsync();
            var day = AttendanceShift.ShapeDay(today, saved);
            var rendered = await RenderedHoursAsync(intern.Id);
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

        private async Task<double> RenderedHoursAsync(int internId)
        {
            var records = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == internId && x.TimeOut != null)
                .Select(x => new { x.TimeIn, x.TimeOut })
                .ToListAsync();

            return records.Sum(x => AttendanceShift.Hours(x.TimeIn, x.TimeOut));
        }
    }
}

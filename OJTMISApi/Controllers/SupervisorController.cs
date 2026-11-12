using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Controllers
{
    /// <summary>
    /// Supervisor endpoints. Every query is scoped to the department of the
    /// signed-in supervisor, resolved from their linked signatory record (or
    /// their user profile office when no signatory is linked).
    /// </summary>
    [ApiController]
    [Route("api/supervisor")]
    [Authorize(Roles = UserRoles.Supervisor)]
    [Produces("application/json")]
    public class SupervisorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly InternCompletionService _completion;
        private readonly ILogger<SupervisorController> _logger;

        public SupervisorController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            InternCompletionService completion,
            ILogger<SupervisorController> logger)
        {
            _context = context;
            _userManager = userManager;
            _completion = completion;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/supervisor/department
        /// Returns the resolved department scope and the office names/codes
        /// used to match records.
        /// </summary>
        [HttpGet("department")]
        public async Task<ActionResult<object>> GetDepartment()
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            return Ok(new
            {
                department = scope.Department,
                officeKeys = scope.OfficeKeys,
                requestIds = scope.RequestIds
            });
        }

        /// <summary>
        /// GET: api/supervisor/interns
        /// Department-filtered intern list. Optional filters: status, search,
        /// and requestId.
        /// </summary>
        [HttpGet("interns")]
        public async Task<ActionResult<IEnumerable<object>>> GetInterns(
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int? requestId)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var query = ApplyInternScope(_context.Interns.AsNoTracking(), scope);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var value = status.Trim();
                query = query.Where(i => i.Status == value);
            }

            if (requestId is int id)
            {
                query = query.Where(i => i.RequestId == id);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(i =>
                    i.FullName.ToLower().Contains(term) ||
                    (i.Email != null && i.Email.ToLower().Contains(term)) ||
                    (i.ApplicantNo != null && i.ApplicantNo.ToLower().Contains(term)));
            }

            var interns = await query
                .OrderBy(i => i.FullName)
                .Select(i => new
                {
                    i.Id,
                    i.ApplicantNo,
                    i.FullName,
                    i.Email,
                    i.ContactNumber,
                    i.SchoolName,
                    i.Program,
                    i.ApplicantOffice,
                    i.Status,
                    i.RequiredHours,
                    i.HoursRendered,
                    i.RequestId,
                    i.HireDate,
                    i.StartDate,
                    i.EndDate,
                    i.CreatedAt
                })
                .ToListAsync();

            return Ok(interns.Select(i => new
            {
                i.Id,
                i.ApplicantNo,
                i.FullName,
                i.Email,
                i.ContactNumber,
                i.SchoolName,
                i.Program,
                i.ApplicantOffice,
                i.Status,
                i.RequiredHours,
                i.HoursRendered,
                remainingHours = Math.Round(Math.Max(0, i.RequiredHours - i.HoursRendered), 2),
                progressPercent = i.RequiredHours > 0
                    ? Math.Round(Math.Min(100, i.HoursRendered / i.RequiredHours * 100), 1)
                    : 0d,
                i.RequestId,
                i.HireDate,
                i.StartDate,
                i.EndDate,
                i.CreatedAt
            }).ToList());
        }

        /// <summary>
        /// GET: api/supervisor/dtr
        /// Department-filtered DTR punches. Optional filters: internId,
        /// from, to, status, and take (1-1000, defaults to 250).
        /// </summary>
        [HttpGet("dtr")]
        public async Task<ActionResult<IEnumerable<object>>> GetDtr(
            [FromQuery] int? internId,
            [FromQuery(Name = "from")] DateTime? fromDate,
            [FromQuery(Name = "to")] DateTime? toDate,
            [FromQuery] string? status,
            [FromQuery] int take = 250)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            take = Math.Clamp(take, 1, 1000);

            var interns = ApplyInternScope(_context.Interns.AsNoTracking(), scope);

            if (internId is int id)
            {
                if (!await interns.AnyAsync(i => i.Id == id))
                {
                    return NotFound(new { message = $"Intern {id} was not found in your department." });
                }

                interns = interns.Where(i => i.Id == id);
            }

            var query = from d in _context.DailyTimeRecords.AsNoTracking()
                        join i in interns on d.InternId equals (int?)i.Id
                        select new
                        {
                            Record = d,
                            InternName = i.FullName,
                            Office = i.ApplicantOffice
                        };

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(x => x.Record.Date >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.Record.Date < end);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var value = status.Trim();
                query = query.Where(x => x.Record.Status == value);
            }

            var records = await query
                .OrderByDescending(x => x.Record.Date)
                .ThenByDescending(x => x.Record.TimeIn)
                .Take(take)
                .ToListAsync();

            return Ok(records.Select(x => new
            {
                x.Record.Id,
                x.Record.InternId,
                x.InternName,
                x.Office,
                x.Record.Date,
                x.Record.ShiftType,
                x.Record.TimeIn,
                x.Record.TimeOut,
                hours = Math.Round(AttendanceShift.Hours(x.Record.TimeIn, x.Record.TimeOut), 2),
                x.Record.Status,
                x.Record.CreatedAt,
                x.Record.UpdatedAt
            }).ToList());
        }

        /// <summary>
        /// GET: api/supervisor/time-adjustments
        /// Department-filtered time adjustment requests. Defaults to Pending;
        /// pass status=All to include every state.
        /// </summary>
        [HttpGet("time-adjustments")]
        public async Task<ActionResult<IEnumerable<object>>> GetTimeAdjustments(
            [FromQuery] string? status = AttendanceAdjustmentStatus.Pending)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var interns = ApplyInternScope(_context.Interns.AsNoTracking(), scope);

            var query = from r in _context.AttendanceAdjustmentRequests.AsNoTracking()
                        join i in interns on r.InternId equals i.Id
                        join d in _context.DailyTimeRecords.AsNoTracking()
                            on r.AttendanceLogId equals d.Id into logs
                        from log in logs.DefaultIfEmpty()
                        select new
                        {
                            Request = r,
                            InternName = i.FullName,
                            Office = i.ApplicantOffice,
                            Log = log
                        };

            if (!string.IsNullOrWhiteSpace(status) &&
                !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var value = status.Trim();
                query = query.Where(x => x.Request.Status == value);
            }

            var rows = await query
                .OrderBy(x => x.Request.RequestedAt)
                .ToListAsync();

            return Ok(rows.Select(x => new
            {
                x.Request.Id,
                x.Request.InternId,
                x.InternName,
                x.Office,
                x.Request.AttendanceLogId,
                x.Request.TargetField,
                x.Request.Reason,
                x.Request.RequestedTime,
                currentTimeIn = x.Log?.TimeIn,
                currentTimeOut = x.Log?.TimeOut,
                x.Request.Status,
                x.Request.RequestedAt,
                x.Request.ReviewedBy,
                x.Request.ApproverSignatoryId,
                x.Request.ReviewedAt
            }).ToList());
        }

        /// <summary>
        /// POST: api/supervisor/time-adjustments/{id}/review
        /// Approves or rejects one pending time adjustment request for an
        /// intern in the supervisor's department. Body accepts either
        /// { "decision": "Approve" } or { "status": "Rejected" }.
        /// </summary>
        [HttpPost("time-adjustments/{id:int}/review")]
        public async Task<ActionResult<object>> ReviewTimeAdjustment(
            int id,
            [FromBody] ReviewTimeAdjustmentInput? input)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var decision = (input?.Decision ?? input?.Status ?? string.Empty).Trim();
            var approve = decision.Equals("approve", StringComparison.OrdinalIgnoreCase) ||
                          decision.Equals("approved", StringComparison.OrdinalIgnoreCase);
            var reject = decision.Equals("reject", StringComparison.OrdinalIgnoreCase) ||
                         decision.Equals("rejected", StringComparison.OrdinalIgnoreCase);

            if (!approve && !reject)
            {
                return BadRequest(new { message = "Decision must be Approve or Reject." });
            }

            var adjustment = await _context.AttendanceAdjustmentRequests
                .Include(r => r.Intern)
                .Include(r => r.AttendanceLog)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (adjustment is null)
            {
                return NotFound(new { message = $"Time adjustment request {id} was not found." });
            }

            var intern = adjustment.Intern
                ?? (adjustment.AttendanceLog?.InternId is int logInternId
                    ? await _context.Interns.FirstOrDefaultAsync(i => i.Id == logInternId)
                    : null);

            if (!IsInternInScope(intern, scope))
            {
                // Do not reveal that a request exists outside the supervisor's
                // department.
                return NotFound(new { message = $"Time adjustment request {id} was not found." });
            }

            if (!string.Equals(adjustment.Status, AttendanceAdjustmentStatus.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = $"This request has already been {adjustment.Status.ToLowerInvariant()}." });
            }

            var log = adjustment.AttendanceLog;
            var now = DateTime.Now;
            var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var label = AttendanceAdjustmentField.Label(adjustment.TargetField);
            double? dailyHours = null;
            var completed = false;

            if (approve)
            {
                if (log is null)
                {
                    return BadRequest(new { message = "The linked attendance log no longer exists." });
                }

                var field = AttendanceAdjustmentField.Normalize(adjustment.TargetField);
                if (field is null)
                {
                    return BadRequest(new { message = "The request target field is not valid." });
                }

                var logIsMorning = !string.Equals(log.ShiftType, AttendanceShift.Afternoon, StringComparison.OrdinalIgnoreCase);
                if (AttendanceAdjustmentField.IsMorning(field) != logIsMorning)
                {
                    return BadRequest(new { message = "The request's target field no longer matches the attendance log." });
                }

                label = AttendanceAdjustmentField.Label(field);
                var approvedTime = log.Date.Date.Add(adjustment.RequestedTime.TimeOfDay);

                if (AttendanceAdjustmentField.IsTimeIn(field))
                {
                    if (log.TimeOut is not null && approvedTime >= log.TimeOut.Value)
                    {
                        return BadRequest(new { message = $"The requested {label} must be before the time out." });
                    }

                    log.TimeIn = approvedTime;
                    log.Status = AttendanceShift.TimeInStatus(log.ShiftType, approvedTime);
                }
                else
                {
                    if (approvedTime <= log.TimeIn)
                    {
                        return BadRequest(new { message = $"The requested {label} must be after the time in." });
                    }

                    log.TimeOut = approvedTime;
                }

                log.UpdatedAt = now;
                adjustment.Status = AttendanceAdjustmentStatus.Approved;
            }
            else
            {
                adjustment.Status = AttendanceAdjustmentStatus.Rejected;
            }

            adjustment.ReviewedBy = reviewerId;
            adjustment.ApproverSignatoryId = await ResolveApproverSignatoryIdAsync(reviewerId);
            adjustment.ReviewedAt = now;

            await NotifyInternAsync(
                intern,
                approve ? "Time adjustment approved" : "Time adjustment rejected",
                approve
                    ? $"Your {label} adjustment for {log!.Date:MMM d, yyyy} was approved by your department supervisor."
                    : $"Your {label} adjustment for {log?.Date.ToString("MMM d, yyyy") ?? "the selected day"} was rejected by your department supervisor.",
                now);

            var internName = intern?.FullName ?? log?.InternName ?? "an intern";
            ActivityLogWriter.Add(
                _context,
                User,
                "POST",
                $"/api/supervisor/time-adjustments/{id}/review",
                approve ? "Approved a time adjustment request" : "Rejected a time adjustment request",
                $"{(approve ? "Approved" : "Rejected")} {internName}'s {label} adjustment for " +
                $"{log?.Date.ToString("MMM d, yyyy") ?? "the selected day"}.");

            await _context.SaveChangesAsync();

            if (approve && log is not null)
            {
                dailyHours = await CalculateDailyHoursAsync(adjustment.InternId, log.Date);
                completed = await _completion.CheckAsync(adjustment.InternId);
            }

            return Ok(new
            {
                message = approve
                    ? "Time adjustment approved."
                    : "Time adjustment rejected.",
                completed,
                request = new
                {
                    adjustment.Id,
                    adjustment.Status,
                    adjustment.ReviewedAt,
                    adjustment.TargetField,
                    adjustment.ApproverSignatoryId
                },
                attendanceLog = log is null
                    ? null
                    : new
                    {
                        log.Id,
                        log.Date,
                        log.ShiftType,
                        log.TimeIn,
                        log.TimeOut,
                        log.Status
                    },
                dailyHours
            });
        }

        /// <summary>
        /// GET: api/supervisor/applicants
        /// Applicant roster for the supervisor's department. Optional filters:
        /// status (Pending, Accepted, Rejected, Hired, ForOrientation) and search.
        /// </summary>
        [HttpGet("applicants")]
        public async Task<ActionResult<IEnumerable<object>>> GetApplicants(
            [FromQuery] string? status,
            [FromQuery] string? search)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var query = ApplyApplicantScope(_context.Applicants.AsNoTracking(), scope);

            if (!string.IsNullOrWhiteSpace(status))
            {
                switch (status.Trim().ToLowerInvariant())
                {
                    case "pending":
                        query = query.Where(a => !a.Accepted && !a.Rejected && !a.Hired);
                        break;
                    case "accepted":
                        query = query.Where(a => a.Accepted && !a.Hired);
                        break;
                    case "rejected":
                        query = query.Where(a => a.Rejected);
                        break;
                    case "hired":
                        query = query.Where(a => a.Hired);
                        break;
                    case "fororientation":
                    case "for-orientation":
                        query = query.Where(a => a.Accepted && !a.Hired && a.OrientationDate == null);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(a =>
                    a.FirstName.ToLower().Contains(term) ||
                    a.LastName.ToLower().Contains(term) ||
                    (a.ApplicantNo != null && a.ApplicantNo.ToLower().Contains(term)) ||
                    (a.Email != null && a.Email.ToLower().Contains(term)) ||
                    (a.SchoolName != null && a.SchoolName.ToLower().Contains(term)));
            }

            var applicants = await query
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .Select(a => new
                {
                    a.Id,
                    a.ApplicantNo,
                    a.FirstName,
                    a.MiddleName,
                    a.LastName,
                    a.Suffix,
                    a.Email,
                    a.ContactNumber,
                    a.SchoolName,
                    a.EducationLevel,
                    a.Program,
                    a.RequiredHours,
                    a.ApplicantOffice,
                    a.Accepted,
                    a.Rejected,
                    a.Hired,
                    a.RequestNo,
                    a.RequestNoLabel,
                    a.ActionDate,
                    a.OrientationDate,
                    a.OrientationTime,
                    a.OrientationOffice,
                    a.OrientationConfirmed,
                    a.HireDate,
                    a.StartDate,
                    a.EndDate,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(applicants.Select(a => new
            {
                a.Id,
                a.ApplicantNo,
                a.FirstName,
                a.MiddleName,
                a.LastName,
                a.Suffix,
                fullName = BuildFullName(a.FirstName, a.MiddleName, a.LastName, a.Suffix),
                a.Email,
                a.ContactNumber,
                a.SchoolName,
                a.EducationLevel,
                a.Program,
                a.RequiredHours,
                office = string.IsNullOrWhiteSpace(a.ApplicantOffice) ? a.OrientationOffice : a.ApplicantOffice,
                a.ApplicantOffice,
                a.Accepted,
                a.Rejected,
                a.Hired,
                status = StatusOf(a.Accepted, a.Rejected, a.Hired),
                a.RequestNo,
                a.RequestNoLabel,
                a.ActionDate,
                orientation = new
                {
                    date = a.OrientationDate,
                    time = a.OrientationTime,
                    office = a.OrientationOffice,
                    confirmed = a.OrientationConfirmed
                },
                a.HireDate,
                a.StartDate,
                a.EndDate,
                a.CreatedAt
            }).ToList());
        }

        /// <summary>
        /// GET: api/supervisor/orientations
        /// Orientation schedules for applicants in the supervisor's department.
        /// Optional filters: from, to, confirmed, and upcomingOnly.
        /// </summary>
        [HttpGet("orientations")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrientations(
            [FromQuery(Name = "from")] DateTime? fromDate,
            [FromQuery(Name = "to")] DateTime? toDate,
            [FromQuery] bool? confirmed,
            [FromQuery] bool upcomingOnly = false)
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var query = ApplyApplicantScope(_context.Applicants.AsNoTracking(), scope)
                .Where(a => a.OrientationDate != null);

            if (fromDate.HasValue)
            {
                var start = fromDate.Value.Date;
                query = query.Where(a => a.OrientationDate >= start);
            }

            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.OrientationDate < end);
            }

            if (confirmed.HasValue)
            {
                query = query.Where(a => a.OrientationConfirmed == confirmed.Value);
            }

            if (upcomingOnly)
            {
                var today = DateTime.Today;
                query = query.Where(a => a.OrientationDate >= today);
            }

            var rows = await query
                .OrderBy(a => a.OrientationDate)
                .ThenBy(a => a.OrientationTime)
                .ThenBy(a => a.LastName)
                .Select(a => new
                {
                    a.Id,
                    a.ApplicantNo,
                    a.FirstName,
                    a.MiddleName,
                    a.LastName,
                    a.Suffix,
                    a.Email,
                    a.ContactNumber,
                    a.SchoolName,
                    a.Program,
                    a.ApplicantOffice,
                    a.OrientationDate,
                    a.OrientationTime,
                    a.OrientationOffice,
                    a.OrientationConfirmed,
                    a.Accepted,
                    a.Hired,
                    a.RequestNoLabel
                })
                .ToListAsync();

            return Ok(rows.Select(a => new
            {
                a.Id,
                a.ApplicantNo,
                fullName = BuildFullName(a.FirstName, a.MiddleName, a.LastName, a.Suffix),
                a.Email,
                a.ContactNumber,
                a.SchoolName,
                a.Program,
                office = string.IsNullOrWhiteSpace(a.OrientationOffice) ? a.ApplicantOffice : a.OrientationOffice,
                date = a.OrientationDate,
                time = a.OrientationTime,
                a.OrientationOffice,
                confirmed = a.OrientationConfirmed,
                a.Accepted,
                a.Hired,
                a.RequestNoLabel
            }).ToList());
        }

        /// <summary>
        /// GET: api/supervisor/slots
        /// Departmental intern request slot allocations, including used,
        /// remaining, assigned applicant, and hired intern counts.
        /// </summary>
        [HttpGet("slots")]
        public async Task<ActionResult<IEnumerable<object>>> GetSlots()
        {
            var scope = await ResolveDepartmentScopeAsync();
            if (scope is null) return MissingDepartment();

            var offices = scope.OfficeKeys
                .Select(key => key.ToLowerInvariant())
                .Distinct()
                .ToList();

            var requests = await _context.InternRequests
                .AsNoTracking()
                .Where(r => offices.Contains(r.OfficeCode.Trim().ToLower()) ||
                            offices.Contains(r.OfficeName.Trim().ToLower()))
                .OrderBy(r => r.OfficeName)
                .ToListAsync();

            var requestIds = requests.Select(r => r.Id).ToList();

            var applicantCounts = requestIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Applicants
                    .AsNoTracking()
                    .Where(a => a.RequestNo != null && requestIds.Contains(a.RequestNo.Value))
                    .GroupBy(a => a.RequestNo!.Value)
                    .Select(g => new { RequestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.RequestId, x => x.Count);

            var hiredCounts = requestIds.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Interns
                    .AsNoTracking()
                    .Where(i => i.RequestId != null &&
                                requestIds.Contains(i.RequestId.Value) &&
                                (i.Status == "Hired" || i.Status == "Completed"))
                    .GroupBy(i => i.RequestId!.Value)
                    .Select(g => new { RequestId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.RequestId, x => x.Count);

            return Ok(requests.Select(r => new
            {
                r.Id,
                r.OfficeCode,
                r.OfficeName,
                r.Count,
                r.Filled,
                remainingSlots = Math.Max(0, r.Count - r.Filled),
                r.Status,
                r.Skills,
                r.Description,
                assignedApplicants = applicantCounts.TryGetValue(r.Id, out var applicants) ? applicants : 0,
                hiredInterns = hiredCounts.TryGetValue(r.Id, out var hired) ? hired : 0,
                r.SignatoryId,
                r.CreatedAt,
                r.UpdatedAt
            }).ToList());
        }

        /* ------------------------------------------------------------------
         * Department scope
         * ---------------------------------------------------------------- */

        private sealed record DepartmentScope(
            string Department,
            IReadOnlyList<string> OfficeKeys,
            IReadOnlyList<int> RequestIds);

        private async Task<DepartmentScope?> ResolveDepartmentScopeAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;

            // A linked signatory record is the authoritative department
            // assignment. User.Office is the fallback for supervisors whose
            // account existed before the signatory link was added.
            var signatoryDepartment = await _context.Signatories
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                            s.Department != null &&
                            s.Department != "")
                .OrderBy(s => s.Id)
                .Select(s => s.Department)
                .FirstOrDefaultAsync();

            var department = !string.IsNullOrWhiteSpace(signatoryDepartment)
                ? signatoryDepartment!.Trim()
                : user.Office?.Trim();

            if (string.IsNullOrWhiteSpace(department)) return null;

            // The directory stores a department name for some accounts and a
            // code for others. Expand the department into both forms using the
            // intern request records so matching works either way.
            var allRequests = await _context.InternRequests
                .AsNoTracking()
                .Select(r => new { r.Id, r.OfficeCode, r.OfficeName })
                .ToListAsync();

            var matchingRequests = allRequests
                .Where(r =>
                    string.Equals(r.OfficeCode, department, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.OfficeName, department, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(r.OfficeName) &&
                     r.OfficeName.Contains(department, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(r.OfficeCode) &&
                     department.Contains(r.OfficeCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var officeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { department };
            foreach (var request in matchingRequests)
            {
                if (!string.IsNullOrWhiteSpace(request.OfficeCode)) officeKeys.Add(request.OfficeCode.Trim());
                if (!string.IsNullOrWhiteSpace(request.OfficeName)) officeKeys.Add(request.OfficeName.Trim());
            }

            return new DepartmentScope(
                department,
                officeKeys.ToList(),
                matchingRequests.Select(r => r.Id).Distinct().ToList());
        }

        private static IQueryable<Intern> ApplyInternScope(IQueryable<Intern> query, DepartmentScope scope)
        {
            var offices = scope.OfficeKeys.Select(key => key.ToLowerInvariant()).Distinct().ToList();
            var requestIds = scope.RequestIds;

            if (requestIds.Count == 0)
            {
                return query.Where(i =>
                    i.ApplicantOffice != null && offices.Contains(i.ApplicantOffice.Trim().ToLower()));
            }

            return query.Where(i =>
                (i.ApplicantOffice != null && offices.Contains(i.ApplicantOffice.Trim().ToLower())) ||
                (i.RequestId != null && requestIds.Contains(i.RequestId.Value)));
        }

        private static IQueryable<Applicant> ApplyApplicantScope(IQueryable<Applicant> query, DepartmentScope scope)
        {
            var offices = scope.OfficeKeys.Select(key => key.ToLowerInvariant()).Distinct().ToList();
            var requestIds = scope.RequestIds;

            if (requestIds.Count == 0)
            {
                return query.Where(a =>
                    (a.ApplicantOffice != null && offices.Contains(a.ApplicantOffice.Trim().ToLower())) ||
                    (a.OrientationOffice != null && offices.Contains(a.OrientationOffice.Trim().ToLower())));
            }

            return query.Where(a =>
                (a.ApplicantOffice != null && offices.Contains(a.ApplicantOffice.Trim().ToLower())) ||
                (a.OrientationOffice != null && offices.Contains(a.OrientationOffice.Trim().ToLower())) ||
                (a.RequestNo != null && requestIds.Contains(a.RequestNo.Value)));
        }

        private static bool IsInternInScope(Intern? intern, DepartmentScope scope)
        {
            if (intern is null) return false;

            if (!string.IsNullOrWhiteSpace(intern.ApplicantOffice) &&
                scope.OfficeKeys.Any(key =>
                    string.Equals(key, intern.ApplicantOffice.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            return intern.RequestId is int requestId && scope.RequestIds.Contains(requestId);
        }

        private async Task<int?> ResolveApproverSignatoryIdAsync(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            return await _context.Signatories
                .AsNoTracking()
                .Where(s => s.UserId == userId &&
                            (s.ApprovalScope == SignatoryApprovalScope.Dtr ||
                             s.ApprovalScope == SignatoryApprovalScope.Both))
                .OrderBy(s => s.Id)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();
        }

        private async Task NotifyInternAsync(Intern? intern, string title, string message, DateTime now)
        {
            if (string.IsNullOrWhiteSpace(intern?.Email)) return;

            var user = await _userManager.FindByEmailAsync(intern.Email);
            if (user is null) return;

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = title,
                Message = message,
                Module = "Attendance",
                Link = "/intern-attendance.html",
                InternId = intern.Id,
                Type = "AttendanceAdjustment",
                IsRead = false,
                CreatedAt = now
            });
        }

        private async Task<double> CalculateDailyHoursAsync(int internId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var records = await _context.DailyTimeRecords
                .AsNoTracking()
                .Where(x => x.InternId == internId && x.Date >= dayStart && x.Date < dayEnd)
                .ToListAsync();

            return Math.Round(records.Sum(x => AttendanceShift.Hours(x)), 2);
        }

        private ObjectResult MissingDepartment() =>
            StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "This supervisor account has no department assigned. Ask HR to link it to a signatory or set the office on the user profile."
            });

        private static string BuildFullName(string? first, string? middle, string? last, string? suffix)
        {
            var parts = new[] { first, middle, last, suffix }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim());

            return string.Join(' ', parts);
        }

        private static string StatusOf(bool accepted, bool rejected, bool hired)
        {
            if (hired) return "Hired";
            if (rejected) return "Rejected";
            if (accepted) return "Accepted";
            return "Pending";
        }
    }

    /// <summary>
    /// Body for the supervisor review action. <see cref="Decision"/> is
    /// preferred; <see cref="Status"/> is accepted as an alias.
    /// </summary>
    public sealed class ReviewTimeAdjustmentInput
    {
        public string? Decision { get; set; }

        public string? Status { get; set; }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJTMISApi.Data;
using OJTMISApi.Models;

namespace OJTMISApi.Services
{
    /// <summary>
    /// The department a supervisor may see, plus every name/code and intern
    /// request id that refers to it.
    /// </summary>
    public sealed record DepartmentScope(
        string Department,
        IReadOnlyList<string> OfficeKeys,
        IReadOnlyList<int> RequestIds);

    /// <summary>Resolves a supervisor's <see cref="DepartmentScope"/>.</summary>
    public sealed class DepartmentScopeService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DepartmentScopeService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>The scope of the given user, or null when they have no department.</summary>
        public async Task<DepartmentScope?> ResolveAsync(string? userId)
        {
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

            var requests = await _context.InternRequests
                .AsNoTracking()
                .Select(r => new InternRequestOffice(r.Id, r.OfficeCode, r.OfficeName))
                .ToListAsync();

            return DepartmentScopeRules.Build(department, requests);
        }
    }

    /// <summary>An intern request's id and office, for scope matching.</summary>
    public sealed record InternRequestOffice(int Id, string? OfficeCode, string? OfficeName);

    /// <summary>Pure matching rules, kept separate so they can be unit tested.</summary>
    public static class DepartmentScopeRules
    {
        /// <summary>
        /// The directory stores a department name for some accounts and a code
        /// for others. Expand the department into both forms using the intern
        /// request records so matching works either way.
        /// </summary>
        public static DepartmentScope Build(string department, IEnumerable<InternRequestOffice> requests)
        {
            var matching = requests
                .Where(r =>
                    string.Equals(r.OfficeCode, department, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(r.OfficeName, department, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(r.OfficeName) &&
                     r.OfficeName.Contains(department, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(r.OfficeCode) &&
                     department.Contains(r.OfficeCode, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var officeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { department };
            foreach (var request in matching)
            {
                if (!string.IsNullOrWhiteSpace(request.OfficeCode)) officeKeys.Add(request.OfficeCode.Trim());
                if (!string.IsNullOrWhiteSpace(request.OfficeName)) officeKeys.Add(request.OfficeName.Trim());
            }

            return new DepartmentScope(
                department,
                officeKeys.ToList(),
                matching.Select(r => r.Id).Distinct().ToList());
        }

        public static IQueryable<Intern> ApplyInternScope(IQueryable<Intern> query, DepartmentScope scope)
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

        public static IQueryable<Applicant> ApplyApplicantScope(IQueryable<Applicant> query, DepartmentScope scope)
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

        public static bool IsInternInScope(Intern? intern, DepartmentScope scope)
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
    }
}

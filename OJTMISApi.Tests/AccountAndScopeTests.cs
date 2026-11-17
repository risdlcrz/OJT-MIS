using OJTMISApi.Models;
using OJTMISApi.Services;

namespace OJTMISApi.Tests
{
    public class AccountActivationTests
    {
        [Fact]
        public void Unverified_email_blocks_sign_in_first()
        {
            var user = new ApplicationUser { EmailConfirmed = false, IsApproved = false };
            Assert.Equal(AccountActivation.EmailUnconfirmedCode, AccountActivation.Blocker(user)?.Code);
        }

        [Fact]
        public void Verified_but_unapproved_waits_for_HR()
        {
            var user = new ApplicationUser { EmailConfirmed = true, IsApproved = false };
            Assert.Equal(AccountActivation.PendingApprovalCode, AccountActivation.Blocker(user)?.Code);
        }

        [Fact]
        public void Both_steps_done_allows_sign_in()
        {
            var user = new ApplicationUser { EmailConfirmed = true, IsApproved = true };
            Assert.Null(AccountActivation.Blocker(user));
        }
    }

    /// <summary>Supervisors must only ever see their own department.</summary>
    public class DepartmentScopeRulesTests
    {
        private static readonly InternRequestOffice[] Requests =
        {
            new(1, "ISDMD", "ISDMD - Information Systems Development"),
            new(2, "HRMO", "Human Resource Management Office"),
            new(3, "ACCT", "Accounting Division")
        };

        [Fact]
        public void A_department_code_expands_to_its_full_name_and_requests()
        {
            var scope = DepartmentScopeRules.Build("ISDMD", Requests);

            Assert.Contains("ISDMD - Information Systems Development", scope.OfficeKeys);
            Assert.Equal(new[] { 1 }, scope.RequestIds);
        }

        [Fact]
        public void A_department_name_matches_by_name()
        {
            var scope = DepartmentScopeRules.Build("Human Resource Management Office", Requests);

            Assert.Contains("HRMO", scope.OfficeKeys);
            Assert.Equal(new[] { 2 }, scope.RequestIds);
        }

        [Fact]
        public void Interns_match_by_office_or_by_request_but_not_other_departments()
        {
            var scope = DepartmentScopeRules.Build("ISDMD", Requests);

            Assert.True(DepartmentScopeRules.IsInternInScope(new Intern { ApplicantOffice = " isdmd " }, scope));
            Assert.True(DepartmentScopeRules.IsInternInScope(new Intern { ApplicantOffice = "", RequestId = 1 }, scope));
            Assert.False(DepartmentScopeRules.IsInternInScope(new Intern { ApplicantOffice = "HRMO", RequestId = 2 }, scope));
            Assert.False(DepartmentScopeRules.IsInternInScope(null, scope));
        }

        [Fact]
        public void Intern_query_filter_keeps_only_the_department()
        {
            var scope = DepartmentScopeRules.Build("ISDMD", Requests);
            var interns = new[]
            {
                new Intern { Id = 1, ApplicantOffice = "ISDMD" },
                new Intern { Id = 2, ApplicantOffice = "Accounting Division" },
                new Intern { Id = 3, ApplicantOffice = null, RequestId = 1 },
                new Intern { Id = 4, ApplicantOffice = null, RequestId = 3 }
            }.AsQueryable();

            var visible = DepartmentScopeRules.ApplyInternScope(interns, scope).Select(i => i.Id).ToList();

            Assert.Equal(new[] { 1, 3 }, visible);
        }
    }
}

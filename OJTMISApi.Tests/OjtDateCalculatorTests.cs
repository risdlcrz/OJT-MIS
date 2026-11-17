using OJTMISApi.Services;

namespace OJTMISApi.Tests
{
    /// <summary>
    /// Documents the current rules: only Friday can be excluded; Saturday and
    /// Sunday count as working days (see the note in the summary for #14).
    /// </summary>
    public class OjtDateCalculatorTests
    {
        private static readonly DateTime Thursday = new(2026, 6, 11);
        private static readonly DateTime Friday = new(2026, 6, 12);

        [Fact]
        public void Start_is_the_hire_date_when_it_is_a_working_day()
        {
            Assert.Equal(Friday, OjtDateCalculator.GetStartDate(Friday, excludeFriday: false));
        }

        [Fact]
        public void Start_moves_past_an_excluded_Friday()
        {
            Assert.Equal(Friday.AddDays(1), OjtDateCalculator.GetStartDate(Friday, excludeFriday: true));
        }

        [Fact]
        public void End_counts_the_start_day_as_day_one()
        {
            Assert.Equal(Thursday, OjtDateCalculator.GetEndDate(Thursday, 1, excludeFriday: false));
            Assert.Equal(Thursday.AddDays(2), OjtDateCalculator.GetEndDate(Thursday, 3, excludeFriday: false));
        }

        [Fact]
        public void End_skips_Fridays_when_excluded()
        {
            // Thu (1), Fri skipped, Sat (2), Sun (3)
            Assert.Equal(Thursday.AddDays(3), OjtDateCalculator.GetEndDate(Thursday, 3, excludeFriday: true));
        }

        [Fact]
        public void Duration_below_one_is_treated_as_one_day()
        {
            Assert.Equal(Thursday, OjtDateCalculator.GetEndDate(Thursday, 0, excludeFriday: false));
        }

        [Fact]
        public void Compute_combines_start_and_end()
        {
            var (start, end) = OjtDateCalculator.Compute(Friday, 2, excludeFriday: true);
            Assert.Equal(Friday.AddDays(1), start);
            Assert.Equal(Friday.AddDays(2), end);
        }
    }
}

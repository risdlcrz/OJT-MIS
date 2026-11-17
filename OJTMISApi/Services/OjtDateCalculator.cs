namespace OJTMISApi.Services
{
    /// <summary>
    /// Works out the estimated start and end date of the OJT from the
    /// hire date, the length of the OJT, and whether Fridays are
    /// excluded.
    /// </summary>
    public static class OjtDateCalculator
    {
        /// <summary>
        /// The first working day is the hire date itself if it is a working
        /// day; otherwise it is the next working day.
        /// </summary>
        public static DateTime GetStartDate(DateTime hireDate, bool excludeFriday)
        {
            var date = hireDate.Date;
            var guard = 0;

            while (!IsWorkingDay(date, excludeFriday))
            {
                date = date.AddDays(1);
                if (++guard > 14) break;   // safety stop for edge cases
            }

            return date;
        }

        /// <summary>
        /// Counts <paramref name="durationDays"/> working days from the
        /// start date (the start date counts). Returns the last day
        /// that was counted.
        /// </summary>
        public static DateTime GetEndDate(DateTime startDate, int durationDays, bool excludeFriday)
        {
            if (durationDays < 1) durationDays = 1;

            var date = startDate;
            var counted = 1;   // the start date itself is day one
            var guard = 0;

            while (counted < durationDays)
            {
                date = date.AddDays(1);
                if (++guard > 100000) break;

                if (IsWorkingDay(date, excludeFriday))
                {
                    counted++;
                }
            }

            return date;
        }

        /// <summary>
        /// A day is a working day unless it is a Friday and
        /// "excludeFriday" is on.
        /// </summary>
        public static bool IsWorkingDay(DateTime date, bool excludeFriday)
        {
            if (excludeFriday && date.DayOfWeek == DayOfWeek.Friday)
            {
                return false;
            }

            return true;
        }

        /// <summary>Works out the start and end date together.</summary>
        public static (DateTime Start, DateTime End) Compute(
            DateTime hireDate, int durationDays, bool excludeFriday)
        {
            var start = GetStartDate(hireDate, excludeFriday);
            var end = GetEndDate(start, durationDays, excludeFriday);
            return (start, end);
        }
    }
}

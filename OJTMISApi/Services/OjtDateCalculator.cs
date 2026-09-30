namespace OJTMISApi.Services
{
    /// <summary>
    /// Nagkakalkula ng tinatantyang (estimated) start at end date ng OJT
    /// batay sa petsa ng pag-hire, haba ng OJT, at kung
    /// ekskluibo ang Biyernes o hindi.
    /// </summary>
    public static class OjtDateCalculator
    {
        /// <summary>
        /// Ang unang "working day" ay ang hire date mismo kung working day
        /// ito, kung hindi ay ang susunod na working day.
        /// </summary>
        public static DateTime GetStartDate(DateTime hireDate, bool excludeFriday)
        {
            var date = hireDate.Date;
            var guard = 0;

            while (!IsWorkingDay(date, excludeFriday))
            {
                date = date.AddDays(1);
                if (++guard > 14) break;   // safety laban sa edge case
            }

            return date;
        }

        /// <summary>
        /// Binibilang ang <paramref name="durationDays"/> working day mula sa
        /// start date (inclusive). Ang return value ay ang huling araw
        /// na nabibilang.
        /// </summary>
        public static DateTime GetEndDate(DateTime startDate, int durationDays, bool excludeFriday)
        {
            if (durationDays < 1) durationDays = 1;

            var date = startDate;
            var counted = 1;   // ang start date mismo ay unang araw
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
        /// Isang araw lang ang working day kapag hindi Biyernes, o kapag
        /// hindi naka-"excludeFriday".
        /// </summary>
        public static bool IsWorkingDay(DateTime date, bool excludeFriday)
        {
            if (excludeFriday && date.DayOfWeek == DayOfWeek.Friday)
            {
                return false;
            }

            return true;
        }

        /// <summary>Kinakalkula ang start at end date nang sabay.</summary>
        public static (DateTime Start, DateTime End) Compute(
            DateTime hireDate, int durationDays, bool excludeFriday)
        {
            var start = GetStartDate(hireDate, excludeFriday);
            var end = GetEndDate(start, durationDays, excludeFriday);
            return (start, end);
        }
    }
}

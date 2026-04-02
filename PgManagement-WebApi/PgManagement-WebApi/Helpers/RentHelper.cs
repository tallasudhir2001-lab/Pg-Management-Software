using PgManagement_WebApi.Models;
using PgManagement_WebApi.Models.NonEntityModels;

namespace PgManagement_WebApi.Helpers
{
    public static class RentHelper
    {
        /// <summary>
        /// Original per-day calculation (backward compatible).
        /// </summary>
        public static List<RentSlice> GetRentSlices(
            DateTime from,
            DateTime to,
            List<RoomRentHistory> rentHistories)
        {
            return GetRentSlices(from, to, rentHistories, "DAILY", stayFromDate: from);
        }

        /// <summary>
        /// Stay-type-aware rent calculation.
        /// MONTHLY: Billing cycles anchored on stayFromDate.Day.
        ///          E.g. stay starts March 29 → cycles are 29 Mar–28 Apr, 29 Apr–28 May, etc.
        ///          Full cycles charge full RentAmount; partial cycles are prorated.
        ///          When isActiveStay is true, the current cycle is charged in full.
        /// DAILY:   per-day calculation (original logic).
        /// </summary>
        public static List<RentSlice> GetRentSlices(
            DateTime from,
            DateTime to,
            List<RoomRentHistory> rentHistories,
            string stayType,
            DateTime stayFromDate,
            bool isActiveStay = false)
        {
            var result = new List<RentSlice>();

            bool isMonthly = string.Equals(stayType, "MONTHLY", StringComparison.OrdinalIgnoreCase);

            var applicableRents = rentHistories
                .Where(r =>
                    r.EffectiveFrom <= to &&
                    (r.EffectiveTo == null || r.EffectiveTo >= from))
                .OrderBy(r => r.EffectiveFrom)
                .ToList();

            if (!applicableRents.Any())
                return result;

            if (isMonthly)
            {
                int cycleDay = stayFromDate.Day;

                // For active stays, extend 'to' to the end of the current billing cycle
                if (isActiveStay)
                {
                    var cycleEnd = GetCycleEnd(to, cycleDay);
                    if (cycleEnd > to) to = cycleEnd;
                }

                var cursor = from;
                while (cursor <= to)
                {
                    var cycleStart = GetCycleStart(cursor, cycleDay);
                    var nextCycleStart = GetNextCycleStart(cycleStart, cycleDay);
                    var cycleEnd = nextCycleStart.AddDays(-1);

                    var periodStart = cursor;
                    var periodEnd = cycleEnd > to ? to : cycleEnd;

                    int totalDaysInCycle = (cycleEnd - cycleStart).Days + 1;
                    int daysInPeriod = (periodEnd - periodStart).Days + 1;
                    bool isFullCycle = (periodStart == cycleStart && periodEnd == cycleEnd);

                    // Use rent effective at cycle start
                    decimal rent = GetEffectiveRent(periodStart, applicableRents);

                    decimal rentPerDay = Decimal.Round(
                        rent / totalDaysInCycle, 2, MidpointRounding.AwayFromZero);
                    decimal amount = isFullCycle
                        ? rent
                        : Decimal.Round(rentPerDay * daysInPeriod, 2, MidpointRounding.AwayFromZero);

                    result.Add(new RentSlice
                    {
                        From = periodStart,
                        To = periodEnd,
                        RentPerDay = rentPerDay,
                        Amount = amount
                    });

                    cursor = periodEnd.AddDays(1);
                }
            }
            else
            {
                // DAILY: per-day calculation using calendar months for rate
                foreach (var rent in applicableRents)
                {
                    var sliceFrom = Max(from, rent.EffectiveFrom);
                    var sliceTo = Min(to, rent.EffectiveTo ?? to);

                    if (sliceFrom > sliceTo) continue;

                    var cursor = sliceFrom;
                    while (cursor <= sliceTo)
                    {
                        var monthStart = new DateTime(cursor.Year, cursor.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                        var periodStart = cursor;
                        var periodEnd = Min(monthEnd, sliceTo);

                        var daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
                        var days = (periodEnd - periodStart).Days + 1;

                        var rentPerDay = Decimal.Round(
                            rent.RentAmount / daysInMonth, 2, MidpointRounding.AwayFromZero);
                        var amount = Decimal.Round(
                            rentPerDay * days, 2, MidpointRounding.AwayFromZero);

                        result.Add(new RentSlice
                        {
                            From = periodStart,
                            To = periodEnd,
                            RentPerDay = rentPerDay,
                            Amount = amount
                        });

                        cursor = periodEnd.AddDays(1);
                    }
                }
            }

            return result;
        }

        // ── Cycle helpers ──────────────────────────────────────────────────

        /// <summary>Find the start of the billing cycle that contains 'date'.</summary>
        public static DateTime GetCycleStart(DateTime date, int cycleDay)
        {
            int adjusted = Math.Min(cycleDay, DateTime.DaysInMonth(date.Year, date.Month));
            if (date.Day >= adjusted)
                return new DateTime(date.Year, date.Month, adjusted);

            var prev = date.AddMonths(-1);
            adjusted = Math.Min(cycleDay, DateTime.DaysInMonth(prev.Year, prev.Month));
            return new DateTime(prev.Year, prev.Month, adjusted);
        }

        /// <summary>Find the start of the next billing cycle after cycleStart.</summary>
        public static DateTime GetNextCycleStart(DateTime cycleStart, int cycleDay)
        {
            var next = cycleStart.AddMonths(1);
            int adjusted = Math.Min(cycleDay, DateTime.DaysInMonth(next.Year, next.Month));
            return new DateTime(next.Year, next.Month, adjusted);
        }

        /// <summary>Find the end of the billing cycle that contains 'date'.</summary>
        public static DateTime GetCycleEnd(DateTime date, int cycleDay)
        {
            var cycleStart = GetCycleStart(date, cycleDay);
            var nextCycleStart = GetNextCycleStart(cycleStart, cycleDay);
            return nextCycleStart.AddDays(-1);
        }

        /// <summary>Get the rent amount effective on a given date.</summary>
        private static decimal GetEffectiveRent(DateTime date, List<RoomRentHistory> rents)
        {
            var rent = rents
                .Where(r => r.EffectiveFrom <= date)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            return rent?.RentAmount ?? rents.First().RentAmount;
        }

        private static DateTime Max(DateTime a, DateTime b)
            => a > b ? a : b;

        private static DateTime Min(DateTime a, DateTime b)
            => a < b ? a : b;
    }
}

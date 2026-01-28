using PgManagement_WebApi.Models;
using PgManagement_WebApi.Models.NonEntityModels;

namespace PgManagement_WebApi.Helpers
{
    public static class RentHelper
    {
        public static List<RentSlice> GetRentSlices(
            DateTime from,
            DateTime to,
            List<RoomRentHistory> rentHistories)
        {
            var result = new List<RentSlice>();

            var applicableRents = rentHistories
                .Where(r =>
                    r.EffectiveFrom <= to &&
                    (r.EffectiveTo == null || r.EffectiveTo >= from))
                .OrderBy(r => r.EffectiveFrom)
                .ToList();

            foreach (var rent in applicableRents)
            {
                var sliceFrom = Max(from, rent.EffectiveFrom);
                var sliceTo = Min(to, rent.EffectiveTo ?? to);

                if (sliceFrom > sliceTo)
                    continue;

                var cursor = sliceFrom;

                while (cursor <= sliceTo)
                {
                    var monthStart = new DateTime(cursor.Year, cursor.Month, 1);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var periodStart = cursor;
                    var periodEnd = Min(monthEnd, sliceTo);

                    var daysInMonth = DateTime.DaysInMonth(
                        periodStart.Year,
                        periodStart.Month
                    );

                    var rentPerDay = Decimal.Round(
                        rent.RentAmount / daysInMonth,
                        2,
                        MidpointRounding.AwayFromZero
                    );

                    var days = (periodEnd - periodStart).Days + 1;

                    var amount = Decimal.Round(
                        rentPerDay * days,
                        2,
                        MidpointRounding.AwayFromZero
                    );

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

            return result;
        }

        private static DateTime Max(DateTime a, DateTime b)
            => a > b ? a : b;

        private static DateTime Min(DateTime a, DateTime b)
            => a < b ? a : b;
    }


}

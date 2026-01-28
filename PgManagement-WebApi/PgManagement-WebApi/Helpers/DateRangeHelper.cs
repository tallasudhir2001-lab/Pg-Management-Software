namespace PgManagement_WebApi.Helpers
{
    public static class DateRangeHelper
    {
        public static List<(DateTime From, DateTime To)> Subtract(
            DateTime rangeFrom,
            DateTime rangeTo,
            IEnumerable<(DateTime From, DateTime To)> subtractRanges)
        {
            var result = new List<(DateTime From, DateTime To)>
        {
            (rangeFrom, rangeTo)
        };

            foreach (var (subFrom, subTo) in subtractRanges)
            {
                result = result.SelectMany(r => SubtractOne(r, subFrom, subTo)).ToList();
            }

            return result;
        }

        private static IEnumerable<(DateTime From, DateTime To)> SubtractOne(
            (DateTime From, DateTime To) range,
            DateTime subFrom,
            DateTime subTo)
        {
            // No overlap
            if (subTo < range.From || subFrom > range.To)
            {
                yield return range;
                yield break;
            }

            // Left side remains
            if (subFrom > range.From)
                yield return (range.From, subFrom.AddDays(-1));

            // Right side remains
            if (subTo < range.To)
                yield return (subTo.AddDays(1), range.To);
        }
    }

}

namespace PgManagement_WebApi.Jobs
{
    public static class ReportTypes
    {
        public const string RentCollection = "RentCollection";
        public const string OverdueRent = "OverdueRent";
        public const string Occupancy = "Occupancy";
        public const string ProfitLoss = "ProfitLoss";
        public const string ExpenseSummary = "ExpenseSummary";
        public const string AdvanceBalance = "AdvanceBalance";

        public static readonly Dictionary<string, (string DisplayName, string Description)> All = new()
        {
            [RentCollection]  = ("Rent Collection",  "Monthly rent collection status with paid/pending breakdown"),
            [OverdueRent]     = ("Overdue Rent",     "Tenants with overdue rent — includes days overdue and outstanding amount"),
            [Occupancy]       = ("Occupancy",        "Room-wise occupancy status with vacancy details"),
            [ProfitLoss]      = ("Profit & Loss",    "Monthly revenue vs expenses with net profit/loss"),
            [ExpenseSummary]  = ("Expense Summary",  "Category-wise expense breakdown for the month"),
            [AdvanceBalance]  = ("Advance Balance",  "Advance deposits held, refunded and net balance per tenant"),
        };
    }
}

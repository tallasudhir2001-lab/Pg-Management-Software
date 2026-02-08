namespace PgManagement_WebApi.DTOs.Expense
{
    public class ExpenseListItemDto
    {
        public string Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Category { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMode { get; set; }
        public string PaymentModeLabel { get; set; }

        public string Description { get; set; }
    }

}

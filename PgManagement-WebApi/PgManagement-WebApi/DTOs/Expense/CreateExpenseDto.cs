using PgManagement_WebApi.Enums;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.DTOs.Expense
{
    public class CreateExpenseDto
    {
        public int CategoryId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Description { get; set; }
        public string PaymentModeCode { get; set; }
        public string? ReferenceNo { get; set; }
        public bool IsRecurring { get; set; }
        public RecurringFrequency? RecurringFrequency { get; set; }
    }

}

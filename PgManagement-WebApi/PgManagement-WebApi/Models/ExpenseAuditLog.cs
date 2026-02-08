using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{

    public class ExpenseAuditLog
    {
        [Key]
        public string Id { get; set; }

        //removed required to make it optional
        public string? ExpenseId { get; set; }
        public Expense? Expense { get; set; }

        [Required, MaxLength(20)]
        public string Action { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
    }


}

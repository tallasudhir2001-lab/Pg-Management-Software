using PgManagement_WebApi.Enums;
using PgManagement_WebApi.Models.BaseAuditableEntity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class Expense : AuditableEntity
    {
        [Key]
        public string Id { get; set; }   // GUID string

        [Required]
        public string PgId { get; set; }
        public PG Pg { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public ExpenseCategory Category { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ExpenseDate { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }
        [Required, MaxLength(20)]
        public string PaymentModeCode { get; set; }
        public PaymentMode PaymentMode { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        public bool IsRecurring { get; set; }
        public RecurringFrequency? RecurringFrequency { get; set; }
        public ICollection<ExpenseAuditLog> AuditLogs { get; set; }
    }

}

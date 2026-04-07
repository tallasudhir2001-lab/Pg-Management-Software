using PgManagement_WebApi.Models.BaseAuditableEntity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class SalaryPayment : AuditableEntity
    {
        [Key]
        public string SalaryPaymentId { get; set; }

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        public string? BranchId { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required, MaxLength(20)]
        public string ForMonth { get; set; }  // e.g. "2026-04"

        [Required, MaxLength(20)]
        public string PaymentModeCode { get; set; }
        public PaymentMode PaymentMode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

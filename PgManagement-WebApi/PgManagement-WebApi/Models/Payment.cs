using PgManagement_WebApi.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class Payment
    {
        [Key]
        public string PaymentId { get; set; }

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        [Required]
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        // What was actually paid
        [Required]
        public decimal Amount { get; set; }

        // When payment was made
        [Required]
        public DateTime PaymentDate { get; set; }

        // Period this payment covers
        [Required]
        public DateTime PaidFrom { get; set; }

        [Required]
        public DateTime PaidUpto { get; set; }

        // Optional, display-only (e.g. "Jan 2026")
        [MaxLength(20)]
        public string? MonthLabel { get; set; }

        // Frequency metadata (NOT used for recalculation)
        [Required]
        public string PaymentFrequencyCode { get; set; }
        public PaymentFrequency PaymentFrequency { get; set; }

        // Mode (cash / upi / bank)
        [Required]
        [MaxLength(20)]
        public string PaymentModeCode { get; set; }
        public PaymentMode PaymentMode { get; set; }

        [Required]
        public string CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; }

        //  Optional notes / adjustments
        [MaxLength(250)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

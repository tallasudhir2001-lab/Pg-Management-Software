using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class Payment
    {
        [Key]
        public string PaymentId { get; set; } // e.g., "P001"

        [Required]
        public string PgId { get; set; }

        [Required]
        public string TenantId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string Month { get; set; } // e.g., "2026-01"

        [Required]
        [MaxLength(20)]
        public string PaymentModeCode { get; set; } // Cash, Online, etc.

        [ForeignKey("PgId")]
        public PG PG { get; set; }

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; }

        [ForeignKey("PaymentModeCode")]
        public PaymentMode PaymentMode { get; set; }
    }
}

using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Models.BaseAuditableEntity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class Advance : AuditableEntity
    {
        [Key]
        public string AdvanceId { get; set; }

        [Required]
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string? BranchId { get; set; }

        // Original amount paid
        [Required]
        public decimal Amount { get; set; }

        // Settlement
        public decimal? DeductedAmount { get; set; }

        // Computed: Amount - DeductedAmount (DO NOT store)
        // public decimal ReturnedAmount { get; set; }

        public bool IsSettled { get; set; }

        public DateTime PaidDate { get; set; }
        public DateTime? SettledDate { get; set; }

        [MaxLength(250)]
        public string? Notes { get; set; }

        // Audit
        [Required]
        public string CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; }

        public string? SettledByUserId { get; set; }
        public ApplicationUser? SettledByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

using PgManagement_WebApi.Identity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class AuditEvent
    {
        [Key]
        public string Id { get; set; }

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        public string? BranchId { get; set; }

        [Required, MaxLength(40)]
        public string EventType { get; set; }

        [Required, MaxLength(30)]
        public string EntityType { get; set; }

        [Required]
        public string EntityId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [Required]
        public string PerformedByUserId { get; set; }
        public ApplicationUser PerformedByUser { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        public bool IsReviewed { get; set; }
        public string? ReviewedByUserId { get; set; }
        public ApplicationUser? ReviewedByUser { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}

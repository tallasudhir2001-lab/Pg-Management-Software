using PgManagement_WebApi.Models.BaseAuditableEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public enum BookingStatus
    {
        Active,
        Cancelled,
        Terminated  // No show on check-in date
    }

    public class Booking : AuditableEntity
    {
        [Key]
        public string BookingId { get; set; }

        [Required]
        public string PgId { get; set; }

        public string? BranchId { get; set; }

        [Required]
        public string TenantId { get; set; }

        [Required]
        public string RoomId { get; set; }

        [Required]
        public DateTime ScheduledCheckInDate { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Active;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceAmount { get; set; }

        public string? Notes { get; set; }

        [ForeignKey("PgId")]
        public PG PG { get; set; }

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; }

        [ForeignKey("RoomId")]
        public Room Room { get; set; }
    }

}

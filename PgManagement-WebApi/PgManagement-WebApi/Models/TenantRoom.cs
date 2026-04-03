using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class TenantRoom
    {
        [Key]
        public Guid TenantRoomId { get; set; }

        public string TenantId { get; set; }
        public string RoomId { get; set; }
        public string PgId { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Optional date when the tenant has informed they plan to move out.
        /// Used to allow future bookings against the freed bed.
        /// Set to null when: tenant checks out, or management clears it.
        /// </summary>
        public DateTime? ExpectedCheckOutDate { get; set; }

        public string StayType { get; set; } = "MONTHLY"; // MONTHLY or DAILY

        public Tenant Tenant { get; set; }
        public Room Room { get; set; }
    }
}

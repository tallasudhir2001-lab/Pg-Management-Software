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

        public Tenant Tenant { get; set; }
        public Room Room { get; set; }
    }
}

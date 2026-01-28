using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class TenantRentHistory
    {
        public Guid TenantRentHistoryId { get; set; }

        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public Guid RoomRentHistoryId { get; set; }   
        public RoomRentHistory RoomRentHistory { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}

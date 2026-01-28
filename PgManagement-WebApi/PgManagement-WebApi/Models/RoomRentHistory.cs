using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class RoomRentHistory
    {
        public Guid RoomRentHistoryId { get; set; }  

        public string RoomId { get; set; }
        public Room Room { get; set; }

        public decimal RentAmount { get; set; }
        public bool IsAc { get; set; }

        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}

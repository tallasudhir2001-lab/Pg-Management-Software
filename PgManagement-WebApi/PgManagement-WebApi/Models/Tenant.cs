using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class Tenant
    {
        [Key]
        public string TenantId { get; set; }  // e.g., "T001"

        [Required]
        public string PgId { get; set; }      // Tenant belongs to PG

        [Required]
        public string Name { get; set; }

        public string RoomId { get; set; }    // Optional: assigned room

        public DateTime CheckInDate { get; set; } = DateTime.UtcNow;
        public DateTime? CheckOutDate { get; set; }

        [MaxLength(15)]
        public string ContactNumber { get; set; }
        public bool isActive { get; set; } 

        [ForeignKey("PgId")]
        public PG PG { get; set; }

        [ForeignKey("RoomId")]
        public Room Room { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class Room
    {
        [Key]
        public string RoomId { get; set; } // e.g., "R001"

        [Required]
        public string PgId { get; set; }   // Foreign key to PG

        [Required]
        public string RoomNumber { get; set; }

        public int Capacity { get; set; } = 1;

        public decimal RentAmount { get; set; }

        [NotMapped]
        public int Vacancies =>
                    Capacity - Tenants.Count(t => t.isActive);

        public bool isAc {  get; set; }
        // Navigation property
        [ForeignKey("PgId")]
        public PG PG { get; set; }

        public ICollection<Tenant> Tenants { get; set; }
    }
}

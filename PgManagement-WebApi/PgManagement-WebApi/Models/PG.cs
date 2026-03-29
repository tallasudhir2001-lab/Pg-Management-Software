using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class PG
    {
        [Key]
        public string PgId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(15)]
        public string ContactNumber { get; set; }

        public string? BranchId { get; set; }
        public Branch? Branch { get; set; }

        // Navigation properties
        public ICollection<Room> Rooms { get; set; }
        public ICollection<Tenant> Tenants { get; set; }
    }
}

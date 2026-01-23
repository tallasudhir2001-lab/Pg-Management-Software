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

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        [MaxLength(10)]
        public string ContactNumber { get; set; }
        [RegularExpression(@"^\d{12}$",
            ErrorMessage = "Aadhaar must be 12 digits")]
        public string AadharNumber { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public DateTime? RentPaidUpto { get; set; }
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }


        [ForeignKey("PgId")]
        public PG PG { get; set; }

        [ForeignKey("RoomId")]
        public Room Room { get; set; }

        public ICollection<Payment> Payments { get; set; }
    }
}

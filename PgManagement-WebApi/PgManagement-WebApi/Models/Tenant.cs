using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.Models
{
    public class Tenant
    {
        [Key]
        public string TenantId { get; set; } 

        [Required]
        public string PgId { get; set; }   

        [Required]
        public string Name { get; set; }


        [Required]
        [RegularExpression(@"^[6-9]\d{9}$",
            ErrorMessage = "Enter a valid 10-digit Indian mobile number")]
        [MaxLength(10)]
        public string ContactNumber { get; set; }
        [RegularExpression(@"^\d{12}$",
            ErrorMessage = "Aadhaar must be 12 digits")]
        public string AadharNumber { get; set; }
        public string? Notes { get; set; }

        [MaxLength(254)]
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool isDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }


        [ForeignKey("PgId")]
        public PG PG { get; set; }


        public ICollection<Payment> Payments { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}

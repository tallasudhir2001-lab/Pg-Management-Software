using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class PaymentMode
    {
        [Key]
        [MaxLength(20)]
        public string Code { get; set; }  // e.g., "UPI", "CASH", "BANK"

        [Required]
        [MaxLength(50)]
        public string Description { get; set; }  // e.g., "UPI Transfer", "Cash", "Bank Transfer"

        // Navigation property
        public ICollection<Payment> Payments { get; set; }
    }
}

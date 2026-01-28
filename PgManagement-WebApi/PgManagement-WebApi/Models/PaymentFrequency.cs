using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class PaymentFrequency
    {
        [Key]
        [MaxLength(20)]
        public string Code { get; set; }
        // MONTHLY, DAILY, CUSTOM

        [Required]
        [MaxLength(50)]
        public string Description { get; set; }

        // For UI hints only
        public bool RequiresUnitCount { get; set; }
        // MONTHLY → true (months)
        // DAILY → true (days)
        // CUSTOM → false

        public ICollection<Payment> Payments { get; set; }
    }
}

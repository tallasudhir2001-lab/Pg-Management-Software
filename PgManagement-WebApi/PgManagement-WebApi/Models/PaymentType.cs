using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class PaymentType
    {
        [Key]
        [MaxLength(50)]
        public string Code { get; set; } // RENT, ADVANCE_PAYMENT, ADVANCE_REFUND

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }

}

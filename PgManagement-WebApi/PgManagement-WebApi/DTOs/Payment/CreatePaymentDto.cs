using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public string TenantId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        // Defaults to today if not sent
        public DateTime? PaymentDate { get; set; }

        // Derived in UI but validated in backend
        [Required]
        public DateTime PaidUpto { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentFrequencyCode { get; set; }
        // MONTHLY / DAILY / CUSTOM

        [Required]
        [MaxLength(20)]
        public string PaymentModeCode { get; set; }

        [MaxLength(250)]
        public string? Notes { get; set; }
    }

}

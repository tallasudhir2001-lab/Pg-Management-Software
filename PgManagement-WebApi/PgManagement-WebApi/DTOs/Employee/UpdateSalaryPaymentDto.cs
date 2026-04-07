using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Employee
{
    public class UpdateSalaryPaymentDto
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required, MaxLength(20)]
        public string ForMonth { get; set; }

        [Required, MaxLength(20)]
        public string PaymentModeCode { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

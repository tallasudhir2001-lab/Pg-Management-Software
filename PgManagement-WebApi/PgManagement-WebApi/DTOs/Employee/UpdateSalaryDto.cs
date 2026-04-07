using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Employee
{
    public class UpdateSalaryDto
    {
        [Required]
        public decimal NewAmount { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }
    }
}

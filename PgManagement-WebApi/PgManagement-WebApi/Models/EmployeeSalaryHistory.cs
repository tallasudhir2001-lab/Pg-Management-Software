using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class EmployeeSalaryHistory
    {
        [Key]
        public Guid EmployeeSalaryHistoryId { get; set; }

        [Required]
        public string EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
    }
}

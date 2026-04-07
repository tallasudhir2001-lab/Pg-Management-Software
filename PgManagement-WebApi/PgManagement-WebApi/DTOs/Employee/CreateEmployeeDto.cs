using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Employee
{
    public class CreateEmployeeDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(10)]
        public string? ContactNumber { get; set; }

        [MaxLength(30)]
        public string? RoleCode { get; set; }

        public DateTime JoinDate { get; set; }

        [Required]
        public decimal Salary { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}

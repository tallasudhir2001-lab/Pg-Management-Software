using PgManagement_WebApi.Models.BaseAuditableEntity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class Employee : AuditableEntity
    {
        [Key]
        public string EmployeeId { get; set; }

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        public string? BranchId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(10)]
        public string? ContactNumber { get; set; }

        [MaxLength(30)]
        public string? RoleCode { get; set; }
        public EmployeeRole? EmployeeRole { get; set; }

        public DateTime JoinDate { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public ICollection<EmployeeSalaryHistory> SalaryHistories { get; set; }
        public ICollection<SalaryPayment> SalaryPayments { get; set; }
    }
}

namespace PgManagement_WebApi.DTOs.Employee
{
    public class EmployeeDetailsDto
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? RoleCode { get; set; }
        public string? RoleName { get; set; }
        public DateTime JoinDate { get; set; }
        public decimal? CurrentSalary { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SalaryHistoryItemDto> SalaryHistory { get; set; } = new();
    }

    public class SalaryHistoryItemDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
    }
}

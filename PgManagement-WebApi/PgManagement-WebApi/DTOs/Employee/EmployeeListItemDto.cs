namespace PgManagement_WebApi.DTOs.Employee
{
    public class EmployeeListItemDto
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? RoleCode { get; set; }
        public string? RoleName { get; set; }
        public DateTime JoinDate { get; set; }
        public decimal? CurrentSalary { get; set; }
        public bool IsActive { get; set; }
    }
}

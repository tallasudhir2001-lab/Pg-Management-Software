namespace PgManagement_WebApi.DTOs.Employee
{
    public class UpdateEmployeeDto
    {
        public string? Name { get; set; }
        public string? ContactNumber { get; set; }
        public string? RoleCode { get; set; }
        public decimal? Salary { get; set; }
        public bool? IsActive { get; set; }
        public string? Notes { get; set; }
    }
}

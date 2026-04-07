namespace PgManagement_WebApi.DTOs.Employee
{
    public class EmployeeListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } = "Name";
        public string? SortDir { get; set; } = "asc";
    }
}

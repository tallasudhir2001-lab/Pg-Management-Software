using PgManagement_WebApi.DTOs.Employee;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeRoleDto>> GetRolesAsync();
        Task<PageResultsDto<EmployeeListItemDto>> GetEmployeesAsync(List<string> pgIds, EmployeeListQueryDto query);
        Task<EmployeeDetailsDto?> GetEmployeeByIdAsync(List<string> pgIds, string employeeId);
        Task<string> CreateEmployeeAsync(string pgId, CreateEmployeeDto dto, string? branchId = null);
        Task UpdateEmployeeAsync(string employeeId, UpdateEmployeeDto dto, List<string> pgIds);
        Task DeleteEmployeeAsync(string employeeId, List<string> pgIds);

        // Salary Payments
        Task<PageResultsDto<SalaryPaymentListItemDto>> GetSalaryPaymentsAsync(List<string> pgIds, SalaryPaymentListQueryDto query);
        Task<string> CreateSalaryPaymentAsync(string pgId, CreateSalaryPaymentDto dto, string? branchId = null);
        Task UpdateSalaryPaymentAsync(string salaryPaymentId, UpdateSalaryPaymentDto dto, List<string> pgIds);
        Task DeleteSalaryPaymentAsync(string salaryPaymentId, List<string> pgIds);
    }
}

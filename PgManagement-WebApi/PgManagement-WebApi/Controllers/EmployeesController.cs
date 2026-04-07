using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Employee;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/employees")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmployeeService _employeeService;

        public EmployeesController(ApplicationDbContext context, IEmployeeService employeeService)
        {
            _context = context;
            _employeeService = employeeService;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            return Ok(await _employeeService.GetRolesAsync());
        }

        // ===========================
        // Salary Payments
        // ===========================

        [AccessPoint("Employee", "View Salary Payments")]
        [HttpGet("salary-payments")]
        public async Task<IActionResult> GetSalaryPayments([FromQuery] SalaryPaymentListQueryDto query)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _employeeService.GetSalaryPaymentsAsync(pgIds, query));
        }

        [AccessPoint("Employee", "View Employees")]
        [HttpGet]
        public async Task<IActionResult> GetEmployees([FromQuery] EmployeeListQueryDto query)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _employeeService.GetEmployeesAsync(pgIds, query));
        }

        [AccessPoint("Employee", "View Employee Details")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            var employee = await _employeeService.GetEmployeeByIdAsync(pgIds, id);
            if (employee == null) return NotFound();
            return Ok(employee);
        }

        [AccessPoint("Employee", "Create Employee")]
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var id = await _employeeService.CreateEmployeeAsync(pgId, dto, branchId);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }

        [AccessPoint("Employee", "Update Employee")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto dto)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            await _employeeService.UpdateEmployeeAsync(id, dto, pgIds);
            return NoContent();
        }

        [AccessPoint("Employee", "Delete Employee")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            await _employeeService.DeleteEmployeeAsync(id, pgIds);
            return NoContent();
        }

        [AccessPoint("Employee", "Create Salary Payment")]
        [HttpPost("salary-payments")]
        public async Task<IActionResult> CreateSalaryPayment([FromBody] CreateSalaryPaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var id = await _employeeService.CreateSalaryPaymentAsync(pgId, dto, branchId);
            return CreatedAtAction(nameof(GetSalaryPayments), new { id }, null);
        }

        [AccessPoint("Employee", "Delete Salary Payment")]
        [HttpDelete("salary-payments/{id}")]
        public async Task<IActionResult> DeleteSalaryPayment(string id)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            await _employeeService.DeleteSalaryPaymentAsync(id, pgIds);
            return NoContent();
        }

        [AccessPoint("Employee", "Update Salary Payment")]
        [HttpPut("salary-payments/{id}")]
        public async Task<IActionResult> UpdateSalaryPayment(string id, [FromBody] UpdateSalaryPaymentDto dto)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            await _employeeService.UpdateSalaryPaymentAsync(id, dto, pgIds);
            return NoContent();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Employee;
using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeRoleDto>> GetRolesAsync()
        {
            return await _context.EmployeeRoles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new EmployeeRoleDto
                {
                    Code = r.Code,
                    Name = r.Name
                })
                .ToListAsync();
        }

        public async Task<PageResultsDto<EmployeeListItemDto>> GetEmployeesAsync(List<string> pgIds, EmployeeListQueryDto query)
        {
            var employeesQuery = _context.Employees
                .AsNoTracking()
                .Where(e => pgIds.Contains(e.PgId));

            // Filters
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                employeesQuery = employeesQuery.Where(e =>
                    e.Name.ToLower().Contains(search) ||
                    (e.EmployeeRole != null && e.EmployeeRole.Name.ToLower().Contains(search)) ||
                    (e.ContactNumber != null && e.ContactNumber.Contains(search)));
            }

            if (query.IsActive.HasValue)
                employeesQuery = employeesQuery.Where(e => e.IsActive == query.IsActive.Value);

            var totalCount = await employeesQuery.CountAsync();

            // Sorting
            employeesQuery = query.SortBy?.ToLower() switch
            {
                "joindate" => query.SortDir == "asc"
                    ? employeesQuery.OrderBy(e => e.JoinDate)
                    : employeesQuery.OrderByDescending(e => e.JoinDate),
                "role" => query.SortDir == "asc"
                    ? employeesQuery.OrderBy(e => e.EmployeeRole != null ? e.EmployeeRole.Name : null)
                    : employeesQuery.OrderByDescending(e => e.EmployeeRole != null ? e.EmployeeRole.Name : null),
                _ => query.SortDir == "asc"
                    ? employeesQuery.OrderBy(e => e.Name)
                    : employeesQuery.OrderByDescending(e => e.Name)
            };

            // Pagination + Projection
            var items = await employeesQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(e => new EmployeeListItemDto
                {
                    EmployeeId = e.EmployeeId,
                    Name = e.Name,
                    ContactNumber = e.ContactNumber,
                    RoleCode = e.RoleCode,
                    RoleName = e.EmployeeRole != null ? e.EmployeeRole.Name : null,
                    JoinDate = e.JoinDate,
                    CurrentSalary = e.SalaryHistories
                        .Where(sh => sh.EffectiveTo == null)
                        .Select(sh => (decimal?)sh.Amount)
                        .FirstOrDefault(),
                    IsActive = e.IsActive
                })
                .ToListAsync();

            return new PageResultsDto<EmployeeListItemDto>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<EmployeeDetailsDto?> GetEmployeeByIdAsync(List<string> pgIds, string employeeId)
        {
            return await _context.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == employeeId && pgIds.Contains(e.PgId))
                .Select(e => new EmployeeDetailsDto
                {
                    EmployeeId = e.EmployeeId,
                    Name = e.Name,
                    ContactNumber = e.ContactNumber,
                    RoleCode = e.RoleCode,
                    RoleName = e.EmployeeRole != null ? e.EmployeeRole.Name : null,
                    JoinDate = e.JoinDate,
                    CurrentSalary = e.SalaryHistories
                        .Where(sh => sh.EffectiveTo == null)
                        .Select(sh => (decimal?)sh.Amount)
                        .FirstOrDefault(),
                    IsActive = e.IsActive,
                    Notes = e.Notes,
                    CreatedAt = e.CreatedAt,
                    SalaryHistory = e.SalaryHistories
                        .OrderByDescending(sh => sh.EffectiveFrom)
                        .Select(sh => new SalaryHistoryItemDto
                        {
                            Id = sh.EmployeeSalaryHistoryId,
                            Amount = sh.Amount,
                            EffectiveFrom = sh.EffectiveFrom,
                            EffectiveTo = sh.EffectiveTo
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<string> CreateEmployeeAsync(string pgId, CreateEmployeeDto dto, string? branchId = null)
        {
            var employeeId = Guid.NewGuid().ToString();

            var employee = new Employee
            {
                EmployeeId = employeeId,
                PgId = pgId,
                BranchId = branchId,
                Name = dto.Name,
                ContactNumber = dto.ContactNumber,
                RoleCode = dto.RoleCode,
                JoinDate = dto.JoinDate,
                IsActive = true,
                Notes = dto.Notes
            };

            _context.Employees.Add(employee);

            // Create initial salary history
            var salaryHistory = new EmployeeSalaryHistory
            {
                EmployeeSalaryHistoryId = Guid.NewGuid(),
                EmployeeId = employeeId,
                Amount = dto.Salary,
                EffectiveFrom = dto.JoinDate,
                EffectiveTo = null
            };

            _context.EmployeeSalaryHistories.Add(salaryHistory);
            await _context.SaveChangesAsync();

            return employeeId;
        }

        public async Task UpdateEmployeeAsync(string employeeId, UpdateEmployeeDto dto, List<string> pgIds)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && pgIds.Contains(e.PgId));

            if (employee == null)
                throw new KeyNotFoundException("Employee not found.");

            if (dto.Name != null) employee.Name = dto.Name;
            if (dto.ContactNumber != null) employee.ContactNumber = dto.ContactNumber;
            if (dto.RoleCode != null) employee.RoleCode = dto.RoleCode;
            if (dto.IsActive.HasValue) employee.IsActive = dto.IsActive.Value;
            if (dto.Notes != null) employee.Notes = dto.Notes;

            // Detect salary change and create history record
            if (dto.Salary.HasValue)
            {
                var currentSalary = await _context.EmployeeSalaryHistories
                    .FirstOrDefaultAsync(sh => sh.EmployeeId == employeeId && sh.EffectiveTo == null);

                if (currentSalary == null || currentSalary.Amount != dto.Salary.Value)
                {
                    var today = DateTime.UtcNow.Date;

                    if (currentSalary != null)
                    {
                        currentSalary.EffectiveTo = today.AddDays(-1);
                    }

                    _context.EmployeeSalaryHistories.Add(new EmployeeSalaryHistory
                    {
                        EmployeeSalaryHistoryId = Guid.NewGuid(),
                        EmployeeId = employeeId,
                        Amount = dto.Salary.Value,
                        EffectiveFrom = today,
                        EffectiveTo = null
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteEmployeeAsync(string employeeId, List<string> pgIds)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId && pgIds.Contains(e.PgId));

            if (employee == null)
                throw new KeyNotFoundException("Employee not found.");

            _context.Employees.Remove(employee); // Soft-delete via AuditableEntity
            await _context.SaveChangesAsync();
        }

        // ===========================
        // Salary Payments
        // ===========================

        public async Task<PageResultsDto<SalaryPaymentListItemDto>> GetSalaryPaymentsAsync(List<string> pgIds, SalaryPaymentListQueryDto query)
        {
            var paymentsQuery = _context.SalaryPayments
                .AsNoTracking()
                .Where(sp => pgIds.Contains(sp.PgId));

            // Filters
            if (!string.IsNullOrWhiteSpace(query.EmployeeId))
                paymentsQuery = paymentsQuery.Where(sp => sp.EmployeeId == query.EmployeeId);

            if (!string.IsNullOrWhiteSpace(query.ForMonth))
                paymentsQuery = paymentsQuery.Where(sp => sp.ForMonth == query.ForMonth);

            if (query.FromDate.HasValue)
                paymentsQuery = paymentsQuery.Where(sp => sp.PaymentDate >= query.FromDate.Value);

            if (query.ToDate.HasValue)
                paymentsQuery = paymentsQuery.Where(sp => sp.PaymentDate <= query.ToDate.Value);

            var totalCount = await paymentsQuery.CountAsync();

            // Sorting
            paymentsQuery = query.SortBy?.ToLower() switch
            {
                "amount" => query.SortDir == "asc"
                    ? paymentsQuery.OrderBy(sp => sp.Amount)
                    : paymentsQuery.OrderByDescending(sp => sp.Amount),
                "employeename" => query.SortDir == "asc"
                    ? paymentsQuery.OrderBy(sp => sp.Employee.Name)
                    : paymentsQuery.OrderByDescending(sp => sp.Employee.Name),
                "formonth" => query.SortDir == "asc"
                    ? paymentsQuery.OrderBy(sp => sp.ForMonth)
                    : paymentsQuery.OrderByDescending(sp => sp.ForMonth),
                _ => query.SortDir == "asc"
                    ? paymentsQuery.OrderBy(sp => sp.PaymentDate)
                    : paymentsQuery.OrderByDescending(sp => sp.PaymentDate)
            };

            var totalAmount = await paymentsQuery.SumAsync(sp => (decimal?)sp.Amount) ?? 0;

            // Pagination + Projection
            var items = await paymentsQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(sp => new SalaryPaymentListItemDto
                {
                    SalaryPaymentId = sp.SalaryPaymentId,
                    EmployeeId = sp.EmployeeId,
                    EmployeeName = sp.Employee.Name,
                    Amount = sp.Amount,
                    PaymentDate = sp.PaymentDate,
                    ForMonth = sp.ForMonth,
                    PaymentModeCode = sp.PaymentModeCode,
                    PaymentModeLabel = sp.PaymentMode.Description,
                    Notes = sp.Notes,
                    PaidBy = sp.CreatedBy
                })
                .ToListAsync();

            return new PageResultsDto<SalaryPaymentListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                TotalAmount = totalAmount
            };
        }

        public async Task<string> CreateSalaryPaymentAsync(string pgId, CreateSalaryPaymentDto dto, string? branchId = null)
        {
            // Validate employee exists and belongs to PG
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId && e.PgId == pgId);

            if (employee == null)
                throw new KeyNotFoundException("Employee not found.");

            if (!employee.IsActive)
                throw new InvalidOperationException("Cannot create salary payment for an inactive employee.");

            // Check for duplicate payment for same employee + month
            var duplicate = await _context.SalaryPayments
                .AsNoTracking()
                .AnyAsync(sp => sp.EmployeeId == dto.EmployeeId && sp.ForMonth == dto.ForMonth && sp.PgId == pgId);

            if (duplicate)
                throw new InvalidOperationException($"Salary payment for {dto.ForMonth} already exists for this employee.");

            var salaryPaymentId = Guid.NewGuid().ToString();

            var salaryPayment = new SalaryPayment
            {
                SalaryPaymentId = salaryPaymentId,
                PgId = pgId,
                BranchId = branchId,
                EmployeeId = dto.EmployeeId,
                Amount = dto.Amount,
                PaymentDate = dto.PaymentDate,
                ForMonth = dto.ForMonth,
                PaymentModeCode = dto.PaymentModeCode,
                Notes = dto.Notes
            };

            _context.SalaryPayments.Add(salaryPayment);
            await _context.SaveChangesAsync();

            return salaryPaymentId;
        }

        public async Task DeleteSalaryPaymentAsync(string salaryPaymentId, List<string> pgIds)
        {
            var salaryPayment = await _context.SalaryPayments
                .FirstOrDefaultAsync(sp => sp.SalaryPaymentId == salaryPaymentId && pgIds.Contains(sp.PgId));

            if (salaryPayment == null)
                throw new KeyNotFoundException("Salary payment not found.");

            _context.SalaryPayments.Remove(salaryPayment); // Soft-delete via AuditableEntity
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSalaryPaymentAsync(string salaryPaymentId, UpdateSalaryPaymentDto dto, List<string> pgIds)
        {
            var salaryPayment = await _context.SalaryPayments
                .FirstOrDefaultAsync(sp => sp.SalaryPaymentId == salaryPaymentId && pgIds.Contains(sp.PgId));

            if (salaryPayment == null)
                throw new KeyNotFoundException("Salary payment not found.");

            salaryPayment.Amount = dto.Amount;
            salaryPayment.PaymentDate = dto.PaymentDate;
            salaryPayment.ForMonth = dto.ForMonth;
            salaryPayment.PaymentModeCode = dto.PaymentModeCode;
            salaryPayment.Notes = dto.Notes;

            await _context.SaveChangesAsync();
        }
    }
}

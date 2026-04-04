using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.PaymentType;

namespace PgManagement_WebApi.Services
{
    public class PaymentTypeService : IPaymentTypeService
    {
        private readonly ApplicationDbContext _context;

        public PaymentTypeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaymentTypeDto>> GetPaymentTypesAsync()
        {
            return await _context.PaymentTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new PaymentTypeDto
                {
                    Code = t.Code,
                    Name = t.Name
                })
                .ToListAsync();
        }
    }
}

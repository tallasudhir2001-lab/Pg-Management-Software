using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Payment;

namespace PgManagement_WebApi.Services
{
    public class PaymentModeService : IPaymentModeService
    {
        private readonly ApplicationDbContext _context;

        public PaymentModeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaymentModeDto>> GetPaymentModesAsync()
        {
            return await _context.PaymentModes
                .OrderBy(pm => pm.Code)
                .Select(pm => new PaymentModeDto
                {
                    Code = pm.Code,
                    Description = pm.Description
                })
                .ToListAsync();
        }
    }
}

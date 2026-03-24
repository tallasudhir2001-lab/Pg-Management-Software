using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.DTOs.PaymentType;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payment-types")]
    [ApiController]
    public class PaymentTypesController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public PaymentTypesController(ApplicationDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentTypes()
        {
            var types = await context.PaymentTypes
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new PaymentTypeDto
                {
                    Code = t.Code,
                    Name = t.Name
                })
                .ToListAsync();

            return Ok(types);
        }
    }
}
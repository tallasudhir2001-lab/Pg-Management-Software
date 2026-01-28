using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.Identity;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payment-modes")]
    [ApiController]
    public class PaymentModesController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IConfiguration configuration;
        public PaymentModesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            this.context = context;
            this.userManager = userManager;
            this.configuration = configuration;
        }
        [HttpGet]
        public async Task<IActionResult> GetPaymentModes()
        {
            var modes = await context.PaymentModes
                .OrderBy(pm => pm.Code)
                .Select(pm => new PaymentModeDto
                {
                    Code = pm.Code,
                    Description = pm.Description
                })
                .ToListAsync();

            return Ok(modes);
        }
    }
}

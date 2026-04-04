using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payment-modes")]
    [ApiController]
    public class PaymentModesController : ControllerBase
    {
        private readonly IPaymentModeService _paymentModeService;

        public PaymentModesController(IPaymentModeService paymentModeService)
        {
            _paymentModeService = paymentModeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentModes()
        {
            var modes = await _paymentModeService.GetPaymentModesAsync();
            return Ok(modes);
        }
    }
}

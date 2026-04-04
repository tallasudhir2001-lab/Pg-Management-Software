using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payment-types")]
    [ApiController]
    public class PaymentTypesController : ControllerBase
    {
        private readonly IPaymentTypeService _paymentTypeService;

        public PaymentTypesController(IPaymentTypeService paymentTypeService)
        {
            _paymentTypeService = paymentTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentTypes()
        {
            var types = await _paymentTypeService.GetPaymentTypesAsync();
            return Ok(types);
        }
    }
}
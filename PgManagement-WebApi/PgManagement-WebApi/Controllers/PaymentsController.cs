using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.Helpers;
using PgManagement_WebApi.Services;
using System.Security.Claims;

namespace PgManagement_WebApi.DTOs.Payment
{
    public class SendReceiptDto
    {
        public string RecipientEmail { get; set; } = "";
    }
}

namespace PgManagement_WebApi.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;

        public PaymentsController(
            IPaymentService paymentService,
            IReportService reportService,
            ApplicationDbContext context)
        {
            _paymentService = paymentService;
            _reportService = reportService;
            _context = context;
        }

        [HttpGet("{paymentId}/receipt")]
        public async Task<IActionResult> GetReceipt(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var pdf = await _reportService.GenerateReceiptAsync(paymentId, pgId);
            return File(pdf, "application/pdf", $"Receipt_{paymentId[^6..].ToUpper()}.pdf");
        }

        [HttpPost("{paymentId}/send-receipt")]
        public async Task<IActionResult> SendReceipt(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.SendReceiptAsync(paymentId, pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [HttpPost("{paymentId}/send-receipt-whatsapp")]
        public async Task<IActionResult> SendReceiptWhatsApp(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.SendReceiptWhatsAppAsync(paymentId, pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Payment", "Create Payment")]
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var branchId = User.FindFirst("branchId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.CreatePaymentAsync(dto, pgId, branchId, userId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Payment", "View Pending Rent")]
        [HttpGet("pending/{tenantId}")]
        public async Task<IActionResult> GetPendingRent(string tenantId, [FromQuery] DateTime? asOfDate)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            return Ok(await _paymentService.GetPendingRentAsync(tenantId, pgId, asOfDate));
        }

        [HttpGet("calculate-rent/{tenantId}")]
        public async Task<IActionResult> CalculateRent(
            string tenantId, [FromQuery] DateTime paidFrom, [FromQuery] DateTime paidUpto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.CalculateRentAsync(tenantId, pgId, paidFrom, paidUpto);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Payment", "Create Payment")]
        [HttpGet("context/{tenantId}")]
        public async Task<IActionResult> GetPaymentContext(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.GetPaymentContextAsync(tenantId, pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Payment", "View Payment History")]
        [HttpGet("tenant/{tenantId}")]
        public async Task<IActionResult> GetPaymentHistoryForTenant(string tenantId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();
            return Ok(await _paymentService.GetPaymentHistoryForTenantAsync(tenantId, pgId));
        }

        [AccessPoint("Payment", "View Payment History")]
        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory(
            int page = 1, int pageSize = 10,
            string? search = null, string? mode = null,
            string? tenantId = null, string? userId = null, string? types = null,
            string sortBy = "date", string sortDir = "desc")
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            return Ok(await _paymentService.GetPaymentHistoryAsync(
                pgIds, page, pageSize, search, mode, tenantId, userId, types, sortBy, sortDir));
        }

        [AccessPoint("Payment", "Delete Payment")]
        [HttpDelete("{paymentId}")]
        public async Task<IActionResult> DeletePayment(string paymentId)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.DeletePaymentAsync(paymentId, pgId, userId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }

        [AccessPoint("Payment", "View Payment Details")]
        [HttpGet("{paymentId}")]
        public async Task<IActionResult> GetPayment(string paymentId)
        {
            var pgIds = await this.GetEffectivePgIds(_context);
            if (!pgIds.Any()) return Unauthorized();
            var payment = await _paymentService.GetPaymentAsync(paymentId, pgIds);
            if (payment == null) return NotFound();
            return Ok(payment);
        }

        [AccessPoint("Payment", "Update Payment")]
        [HttpPut("{paymentId}")]
        public async Task<IActionResult> UpdatePayment(string paymentId, [FromBody] UpdatePaymentDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(pgId) || string.IsNullOrEmpty(userId)) return Unauthorized();
            var (success, result, statusCode) = await _paymentService.UpdatePaymentAsync(paymentId, pgId, dto, userId);
            if (!success) return StatusCode(statusCode, result);
            return NoContent();
        }
    }
}

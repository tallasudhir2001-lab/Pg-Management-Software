using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.Reports;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IEmailNotificationService _emailService;

        public ReportsController(IReportService reportService, IEmailNotificationService emailService)
        {
            _reportService = reportService;
            _emailService = emailService;
        }

        private string PgId => User.FindFirst("pgId")?.Value ?? "";

        // ──────────────────────────────────────────────────────────────────────
        // RENT COLLECTION
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Rent Collection Report")]
        [HttpGet("rent-collection")]
        public async Task<IActionResult> RentCollection(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateRentCollectionReportAsync(PgId, d.Month, d.Year, null, status);
            return File(pdf, "application/pdf", $"RentCollection_{d.Year}_{d.Month:D2}.pdf");
        }

        [AccessPoint("Report", "Rent Collection Report")]
        [HttpGet("rent-collection/data")]
        public async Task<IActionResult> RentCollectionData(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var data = await _reportService.GetRentCollectionDataAsync(PgId, d.Month, d.Year, null, status);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // OVERDUE RENT
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Overdue Rent Report")]
        [HttpGet("overdue-rent")]
        public async Task<IActionResult> OverdueRent([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateOverdueRentReportAsync(PgId, asOfDate ?? DateTime.Today, null);
            return File(pdf, "application/pdf", "OverdueRent.pdf");
        }

        [AccessPoint("Report", "Overdue Rent Report")]
        [HttpGet("overdue-rent/data")]
        public async Task<IActionResult> OverdueRentData([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var data = await _reportService.GetOverdueRentDataAsync(PgId, asOfDate ?? DateTime.Today, null);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // PAYMENT HISTORY REPORT
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Payment History Report")]
        [HttpGet("payment-history")]
        public async Task<IActionResult> PaymentHistoryReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? types = null,
            [FromQuery] string? modes = null,
            [FromQuery] string? tenantId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            var pdf = await _reportService.GeneratePaymentHistoryReportAsync(PgId, from, to, types, modes, tenantId);
            return File(pdf, "application/pdf", "PaymentHistory.pdf");
        }

        [AccessPoint("Report", "Payment History Report")]
        [HttpGet("payment-history/data")]
        public async Task<IActionResult> PaymentHistoryData(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? types = null,
            [FromQuery] string? modes = null,
            [FromQuery] string? tenantId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            var data = await _reportService.GetPaymentHistoryDataAsync(PgId, from, to, types, modes, tenantId);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // ROOM OCCUPANCY
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Room Occupancy Report")]
        [HttpGet("occupancy")]
        public async Task<IActionResult> Occupancy([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateOccupancyReportAsync(PgId, asOfDate ?? DateTime.Today);
            return File(pdf, "application/pdf", "RoomOccupancy.pdf");
        }

        [AccessPoint("Report", "Room Occupancy Report")]
        [HttpGet("occupancy/data")]
        public async Task<IActionResult> OccupancyData([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var data = await _reportService.GetOccupancyDataAsync(PgId, asOfDate ?? DateTime.Today);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // TENANT LIST
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Tenant List Report")]
        [HttpGet("tenant-list")]
        public async Task<IActionResult> TenantList(
            [FromQuery] string status = "ACTIVE",
            [FromQuery] string? roomId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateTenantListReportAsync(PgId, status, roomId);
            return File(pdf, "application/pdf", $"TenantList_{status}.pdf");
        }

        [AccessPoint("Report", "Tenant List Report")]
        [HttpGet("tenant-list/data")]
        public async Task<IActionResult> TenantListData(
            [FromQuery] string status = "ACTIVE",
            [FromQuery] string? roomId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var data = await _reportService.GetTenantListDataAsync(PgId, status, roomId);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // ADVANCE BALANCE
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Advance Balance Report")]
        [HttpGet("advance-balance")]
        public async Task<IActionResult> AdvanceBalance()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateAdvanceBalanceReportAsync(PgId);
            return File(pdf, "application/pdf", "AdvanceBalance.pdf");
        }

        [AccessPoint("Report", "Advance Balance Report")]
        [HttpGet("advance-balance/data")]
        public async Task<IActionResult> AdvanceBalanceData()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var data = await _reportService.GetAdvanceBalanceDataAsync(PgId);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // EXPENSE REPORT
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Monthly Expense Report")]
        [HttpGet("expenses")]
        public async Task<IActionResult> Expenses(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? category = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateExpenseReportAsync(PgId, d.Month, d.Year, category);
            return File(pdf, "application/pdf", $"Expenses_{d.Year}_{d.Month:D2}.pdf");
        }

        [AccessPoint("Report", "Monthly Expense Report")]
        [HttpGet("expenses/data")]
        public async Task<IActionResult> ExpensesData(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? category = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var data = await _reportService.GetExpenseReportDataAsync(PgId, d.Month, d.Year, category);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // PROFIT & LOSS
        // ──────────────────────────────────────────────────────────────────────
        [AccessPoint("Report", "Profit & Loss Report")]
        [HttpGet("profit-loss")]
        public async Task<IActionResult> ProfitLoss(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateProfitLossReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"ProfitLoss_{d.Year}_{d.Month:D2}.pdf");
        }

        [AccessPoint("Report", "Profit & Loss Report")]
        [HttpGet("profit-loss/data")]
        public async Task<IActionResult> ProfitLossData(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var data = await _reportService.GetProfitLossDataAsync(PgId, d.Month, d.Year);
            return Ok(data);
        }

        // ──────────────────────────────────────────────────────────────────────
        // SEND REPORT TO OWNER
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("send")]
        public async Task<IActionResult> SendReport([FromBody] SendReportDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (string.IsNullOrEmpty(dto.RecipientEmail))
                return BadRequest("Recipient email is required.");

            var filters = dto.Filters ?? new Dictionary<string, string>();
            filters.TryGetValue("asOfDate", out var asOfStr);
            filters.TryGetValue("fromDate", out var fromStr);
            filters.TryGetValue("status", out var status);
            filters.TryGetValue("category", out var category);
            filters.TryGetValue("types", out var types);
            filters.TryGetValue("modes", out var modes);
            filters.TryGetValue("tenantId", out var tenantId);

            var asOfDate = DateTime.TryParse(asOfStr, out var aof) ? aof : DateTime.Today;
            var fromDate = DateTime.TryParse(fromStr, out var fd) ? fd : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            byte[] pdf = dto.ReportType switch
            {
                "rent-collection"  => await _reportService.GenerateRentCollectionReportAsync(PgId, fromDate.Month, fromDate.Year, null, status),
                "overdue-rent"     => await _reportService.GenerateOverdueRentReportAsync(PgId, asOfDate, null),
                "payment-history"  => await _reportService.GeneratePaymentHistoryReportAsync(PgId, fromDate, DateTime.Today, types, modes, tenantId),
                "occupancy"        => await _reportService.GenerateOccupancyReportAsync(PgId, asOfDate),
                "tenant-list"      => await _reportService.GenerateTenantListReportAsync(PgId, status ?? "ACTIVE", null),
                "advance-balance"  => await _reportService.GenerateAdvanceBalanceReportAsync(PgId),
                "expenses"         => await _reportService.GenerateExpenseReportAsync(PgId, fromDate.Month, fromDate.Year, category),
                "profit-loss"      => await _reportService.GenerateProfitLossReportAsync(PgId, fromDate.Month, fromDate.Year),
                _ => throw new ArgumentException("Unknown report type")
            };

            await _emailService.SendReportAsync(dto.ReportType, pdf, dto.RecipientEmail);
            return Ok(new { message = $"Report sent to {dto.RecipientEmail}" });
        }
    }
}

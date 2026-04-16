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
        private readonly IWhatsAppProvider _whatsAppProvider;

        public ReportsController(
            IReportService reportService,
            IEmailNotificationService emailService,
            IWhatsAppProvider whatsAppProvider)
        {
            _reportService = reportService;
            _emailService = emailService;
            _whatsAppProvider = whatsAppProvider;
        }

        private string PgId => User.FindFirst("pgId")?.Value ?? "";

        [AccessPoint("Report", "Rent Collection Report")]
        [HttpGet("rent-collection")]
        public async Task<IActionResult> RentCollection(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateRentCollectionReportAsync(PgId, d.Month, d.Year, null, status);
            return File(pdf, "application/pdf", $"RentCollection_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("rent-collection/data")]
        public async Task<IActionResult> RentCollectionData(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? status = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var data = await _reportService.GetRentCollectionDataAsync(PgId, d.Month, d.Year, null, status);
            return Ok(data);
        }

        [AccessPoint("Report", "Overdue Rent Report")]
        [HttpGet("overdue-rent")]
        public async Task<IActionResult> OverdueRent([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateOverdueRentReportAsync(PgId, d, null);
            return File(pdf, "application/pdf", $"OverdueRent_{d:yyyyMMdd}.pdf");
        }

        [HttpGet("overdue-rent/data")]
        public async Task<IActionResult> OverdueRentData([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            var data = await _reportService.GetOverdueRentDataAsync(PgId, d, null);
            return Ok(data);
        }

        [AccessPoint("Report", "Occupancy Report")]
        [HttpGet("occupancy")]
        public async Task<IActionResult> Occupancy([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateOccupancyReportAsync(PgId, d);
            return File(pdf, "application/pdf", $"Occupancy_{d:yyyyMMdd}.pdf");
        }

        [HttpGet("occupancy/data")]
        public async Task<IActionResult> OccupancyData([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            return Ok(await _reportService.GetOccupancyDataAsync(PgId, d));
        }

        [AccessPoint("Report", "Tenant List Report")]
        [HttpGet("tenant-list")]
        public async Task<IActionResult> TenantList([FromQuery] string status = "ACTIVE")
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateTenantListReportAsync(PgId, status, null);
            return File(pdf, "application/pdf", $"TenantList_{status}.pdf");
        }

        [HttpGet("tenant-list/data")]
        public async Task<IActionResult> TenantListData([FromQuery] string status = "ACTIVE")
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            return Ok(await _reportService.GetTenantListDataAsync(PgId, status, null));
        }

        [AccessPoint("Report", "Advance Balance Report")]
        [HttpGet("advance-balance")]
        public async Task<IActionResult> AdvanceBalance()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var pdf = await _reportService.GenerateAdvanceBalanceReportAsync(PgId);
            return File(pdf, "application/pdf", "AdvanceBalance.pdf");
        }

        [HttpGet("advance-balance/data")]
        public async Task<IActionResult> AdvanceBalanceData()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            return Ok(await _reportService.GetAdvanceBalanceDataAsync(PgId));
        }

        [AccessPoint("Report", "Expense Report")]
        [HttpGet("expenses")]
        public async Task<IActionResult> Expenses(
            [FromQuery] DateTime? fromDate = null, [FromQuery] string? category = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateExpenseReportAsync(PgId, d.Month, d.Year, category);
            return File(pdf, "application/pdf", $"Expenses_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("expenses/data")]
        public async Task<IActionResult> ExpensesData(
            [FromQuery] DateTime? fromDate = null, [FromQuery] string? category = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetExpenseReportDataAsync(PgId, d.Month, d.Year, category));
        }

        [AccessPoint("Report", "Profit & Loss Report")]
        [HttpGet("profit-loss")]
        public async Task<IActionResult> ProfitLoss([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateProfitLossReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"ProfitLoss_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("profit-loss/data")]
        public async Task<IActionResult> ProfitLossData([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetProfitLossDataAsync(PgId, d.Month, d.Year));
        }

        [AccessPoint("Report", "Payment History Report")]
        [HttpGet("payment-history")]
        public async Task<IActionResult> PaymentHistory(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? types = null, [FromQuery] string? modes = null,
            [FromQuery] string? tenantId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            var pdf = await _reportService.GeneratePaymentHistoryReportAsync(PgId, from, to, types, modes, tenantId);
            return File(pdf, "application/pdf", $"PaymentHistory_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }

        [HttpGet("payment-history/data")]
        public async Task<IActionResult> PaymentHistoryData(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? types = null, [FromQuery] string? modes = null,
            [FromQuery] string? tenantId = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            return Ok(await _reportService.GetPaymentHistoryDataAsync(PgId, from, to, types, modes, tenantId));
        }

        // ────────────── NEW REPORTS ──────────────

        [AccessPoint("Report", "Tenant Turnover Report")]
        [HttpGet("tenant-turnover")]
        public async Task<IActionResult> TenantTurnover([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateTenantTurnoverReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"TenantTurnover_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("tenant-turnover/data")]
        public async Task<IActionResult> TenantTurnoverData([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetTenantTurnoverDataAsync(PgId, d.Month, d.Year));
        }

        [AccessPoint("Report", "Room Revenue Report")]
        [HttpGet("room-revenue")]
        public async Task<IActionResult> RoomRevenue([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateRoomRevenueReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"RoomRevenue_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("room-revenue/data")]
        public async Task<IActionResult> RoomRevenueData([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetRoomRevenueDataAsync(PgId, d.Month, d.Year));
        }

        [AccessPoint("Report", "Salary Report")]
        [HttpGet("salary")]
        public async Task<IActionResult> Salary([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateSalaryReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"Salary_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("salary/data")]
        public async Task<IActionResult> SalaryData([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetSalaryReportDataAsync(PgId, d.Month, d.Year));
        }

        [AccessPoint("Report", "Cash Flow Report")]
        [HttpGet("cash-flow")]
        public async Task<IActionResult> CashFlow([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateCashFlowReportAsync(PgId, d.Month, d.Year);
            return File(pdf, "application/pdf", $"CashFlow_{d.Year}_{d.Month:D2}.pdf");
        }

        [HttpGet("cash-flow/data")]
        public async Task<IActionResult> CashFlowData([FromQuery] DateTime? fromDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = fromDate ?? DateTime.Today;
            return Ok(await _reportService.GetCashFlowDataAsync(PgId, d.Month, d.Year));
        }

        [AccessPoint("Report", "Tenant Aging Report")]
        [HttpGet("tenant-aging")]
        public async Task<IActionResult> TenantAging([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateTenantAgingReportAsync(PgId, d);
            return File(pdf, "application/pdf", $"TenantAging_{d:yyyyMMdd}.pdf");
        }

        [HttpGet("tenant-aging/data")]
        public async Task<IActionResult> TenantAgingData([FromQuery] DateTime? asOfDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var d = asOfDate ?? DateTime.Today;
            return Ok(await _reportService.GetTenantAgingDataAsync(PgId, d));
        }

        [AccessPoint("Report", "Room Change History Report")]
        [HttpGet("room-change-history")]
        public async Task<IActionResult> RoomChangeHistory(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateRoomChangeHistoryReportAsync(PgId, from, to);
            return File(pdf, "application/pdf", $"RoomChangeHistory_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }

        [HttpGet("room-change-history/data")]
        public async Task<IActionResult> RoomChangeHistoryData(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            return Ok(await _reportService.GetRoomChangeHistoryDataAsync(PgId, from, to));
        }

        [AccessPoint("Report", "Booking Conversion Report")]
        [HttpGet("booking-conversion")]
        public async Task<IActionResult> BookingConversion(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            var pdf = await _reportService.GenerateBookingConversionReportAsync(PgId, from, to);
            return File(pdf, "application/pdf", $"BookingConversion_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
        }

        [HttpGet("booking-conversion/data")]
        public async Task<IActionResult> BookingConversionData(
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            var from = fromDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var to = toDate ?? DateTime.Today;
            return Ok(await _reportService.GetBookingConversionDataAsync(PgId, from, to));
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendReport([FromBody] SendReportDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (string.IsNullOrEmpty(dto.RecipientEmail))
                return BadRequest("Recipient email is required.");

            var (enabled, error) = await _reportService.CheckEmailSubscriptionAsync(PgId);
            if (!enabled) return BadRequest(error);

            var filters = dto.Filters ?? new Dictionary<string, string>();
            var pdf = await GenerateReportPdf(dto.ReportType, filters);
            await _emailService.SendReportAsync(dto.ReportType, pdf, dto.RecipientEmail);
            return Ok(new { message = $"Report sent to {dto.RecipientEmail}" });
        }

        [HttpPost("send-whatsapp")]
        public async Task<IActionResult> SendReportWhatsApp([FromBody] SendReportWhatsAppDto dto)
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            if (string.IsNullOrEmpty(dto.PhoneNumber))
                return BadRequest("Phone number is required.");

            var (enabled, error) = await _reportService.CheckWhatsAppSubscriptionAsync(PgId);
            if (!enabled) return BadRequest(error);

            var filters = dto.Filters ?? new Dictionary<string, string>();
            var pdf = await GenerateReportPdf(dto.ReportType, filters);
            var caption = $"Report: {dto.ReportType.Replace("-", " ").ToUpper()}\nGenerated on {DateTime.Now:dd MMM yyyy HH:mm}";
            await _whatsAppProvider.SendDocumentAsync(dto.PhoneNumber, pdf, $"{dto.ReportType}.pdf", caption);
            return Ok(new { message = $"Report sent via WhatsApp to {dto.PhoneNumber}" });
        }

        [HttpGet("available-recipients")]
        public async Task<IActionResult> GetAvailableRecipients()
        {
            if (string.IsNullOrEmpty(PgId)) return Unauthorized();
            return Ok(await _reportService.GetAvailableRecipientsAsync(PgId));
        }

        private async Task<byte[]> GenerateReportPdf(string reportType, Dictionary<string, string> filters)
        {
            filters.TryGetValue("asOfDate", out var asOfStr);
            filters.TryGetValue("fromDate", out var fromStr);
            filters.TryGetValue("status", out var status);
            filters.TryGetValue("category", out var category);
            filters.TryGetValue("types", out var types);
            filters.TryGetValue("modes", out var modes);
            filters.TryGetValue("tenantId", out var tenantId);

            var asOfDate = DateTime.TryParse(asOfStr, out var aof) ? aof : DateTime.Today;
            var fromDate = DateTime.TryParse(fromStr, out var fd) ? fd : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            return reportType switch
            {
                "rent-collection"      => await _reportService.GenerateRentCollectionReportAsync(PgId, fromDate.Month, fromDate.Year, null, status),
                "overdue-rent"         => await _reportService.GenerateOverdueRentReportAsync(PgId, asOfDate, null),
                "payment-history"      => await _reportService.GeneratePaymentHistoryReportAsync(PgId, fromDate, DateTime.Today, types, modes, tenantId),
                "occupancy"            => await _reportService.GenerateOccupancyReportAsync(PgId, asOfDate),
                "tenant-list"          => await _reportService.GenerateTenantListReportAsync(PgId, status ?? "ACTIVE", null),
                "advance-balance"      => await _reportService.GenerateAdvanceBalanceReportAsync(PgId),
                "expenses"             => await _reportService.GenerateExpenseReportAsync(PgId, fromDate.Month, fromDate.Year, category),
                "profit-loss"          => await _reportService.GenerateProfitLossReportAsync(PgId, fromDate.Month, fromDate.Year),
                "tenant-turnover"      => await _reportService.GenerateTenantTurnoverReportAsync(PgId, fromDate.Month, fromDate.Year),
                "room-revenue"         => await _reportService.GenerateRoomRevenueReportAsync(PgId, fromDate.Month, fromDate.Year),
                "salary"               => await _reportService.GenerateSalaryReportAsync(PgId, fromDate.Month, fromDate.Year),
                "cash-flow"            => await _reportService.GenerateCashFlowReportAsync(PgId, fromDate.Month, fromDate.Year),
                "tenant-aging"         => await _reportService.GenerateTenantAgingReportAsync(PgId, asOfDate),
                "room-change-history"  => await _reportService.GenerateRoomChangeHistoryReportAsync(PgId, fromDate, DateTime.Today),
                "booking-conversion"   => await _reportService.GenerateBookingConversionReportAsync(PgId, fromDate, DateTime.Today),
                _ => throw new ArgumentException("Unknown report type")
            };
        }
    }
}

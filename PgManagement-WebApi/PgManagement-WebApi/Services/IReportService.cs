using PgManagement_WebApi.DTOs.Reports;

namespace PgManagement_WebApi.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateReceiptAsync(string paymentId, string pgId);
        Task<byte[]> GenerateRentCollectionReportAsync(string pgId, int month, int year, string? roomId, string? status);
        Task<byte[]> GenerateOverdueRentReportAsync(string pgId, DateTime asOfDate, string? roomId);
        Task<byte[]> GeneratePaymentHistoryReportAsync(string pgId, DateTime fromDate, DateTime toDate, string? types, string? modes, string? tenantId);
        Task<byte[]> GenerateOccupancyReportAsync(string pgId, DateTime asOfDate);
        Task<byte[]> GenerateTenantListReportAsync(string pgId, string status, string? roomId);
        Task<byte[]> GenerateAdvanceBalanceReportAsync(string pgId);
        Task<byte[]> GenerateExpenseReportAsync(string pgId, int month, int year, string? categories);
        Task<byte[]> GenerateProfitLossReportAsync(string pgId, int month, int year);
        Task<byte[]> GenerateTenantTurnoverReportAsync(string pgId, int month, int year);
        Task<byte[]> GenerateRoomRevenueReportAsync(string pgId, int month, int year);
        Task<byte[]> GenerateSalaryReportAsync(string pgId, int month, int year);
        Task<byte[]> GenerateCashFlowReportAsync(string pgId, int month, int year);
        Task<byte[]> GenerateTenantAgingReportAsync(string pgId, DateTime asOfDate);
        Task<byte[]> GenerateRoomChangeHistoryReportAsync(string pgId, DateTime fromDate, DateTime toDate);
        Task<byte[]> GenerateBookingConversionReportAsync(string pgId, DateTime fromDate, DateTime toDate);

        // Data variants (JSON)
        Task<RentCollectionReportDto> GetRentCollectionDataAsync(string pgId, int month, int year, string? roomId, string? status);
        Task<OverdueRentReportDto> GetOverdueRentDataAsync(string pgId, DateTime asOfDate, string? roomId);
        Task<OccupancyReportDto> GetOccupancyDataAsync(string pgId, DateTime asOfDate);
        Task<TenantListReportDto> GetTenantListDataAsync(string pgId, string status, string? roomId);
        Task<AdvanceBalanceReportDto> GetAdvanceBalanceDataAsync(string pgId);
        Task<ExpenseReportDto> GetExpenseReportDataAsync(string pgId, int month, int year, string? categories);
        Task<ProfitLossReportDto> GetProfitLossDataAsync(string pgId, int month, int year);
        Task<PaymentHistoryReportDto> GetPaymentHistoryDataAsync(string pgId, DateTime fromDate, DateTime toDate, string? types, string? modes, string? tenantId);
        Task<TenantTurnoverReportDto> GetTenantTurnoverDataAsync(string pgId, int month, int year);
        Task<RoomRevenueReportDto> GetRoomRevenueDataAsync(string pgId, int month, int year);
        Task<SalaryReportDto> GetSalaryReportDataAsync(string pgId, int month, int year);
        Task<CashFlowReportDto> GetCashFlowDataAsync(string pgId, int month, int year);
        Task<TenantAgingReportDto> GetTenantAgingDataAsync(string pgId, DateTime asOfDate);
        Task<RoomChangeHistoryReportDto> GetRoomChangeHistoryDataAsync(string pgId, DateTime fromDate, DateTime toDate);
        Task<BookingConversionReportDto> GetBookingConversionDataAsync(string pgId, DateTime fromDate, DateTime toDate);

        Task<(bool enabled, string? error)> CheckEmailSubscriptionAsync(string pgId);
        Task<(bool enabled, string? error)> CheckWhatsAppSubscriptionAsync(string pgId);
        Task<object> GetAvailableRecipientsAsync(string pgId);
    }
}

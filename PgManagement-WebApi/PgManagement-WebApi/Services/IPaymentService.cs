using PgManagement_WebApi.DTOs.Pagination;
using PgManagement_WebApi.DTOs.Payment;
using PgManagement_WebApi.DTOs.Payment.PendingRent;

namespace PgManagement_WebApi.Services
{
    public interface IPaymentService
    {
        Task<(bool success, object result, int statusCode)> CreatePaymentAsync(
            CreatePaymentDto dto, string pgId, string? branchId, string userId);
        Task<PendingRentResponseDto> GetPendingRentAsync(string tenantId, string pgId, DateTime? asOfDate);
        Task<(bool success, object result, int statusCode)> CalculateRentAsync(
            string tenantId, string pgId, DateTime paidFrom, DateTime paidUpto);
        Task<(bool success, object result, int statusCode)> GetPaymentContextAsync(string tenantId, string pgId);
        Task<object> GetPaymentHistoryForTenantAsync(string tenantId, string pgId);
        Task<PageResultsDto<PaymentHistoryDto>> GetPaymentHistoryAsync(
            List<string> pgIds, int page, int pageSize, string? search, string? mode,
            string? tenantId, string? userId, string? types, string sortBy, string sortDir);
        Task<(bool success, object result, int statusCode)> DeletePaymentAsync(
            string paymentId, string pgId, string userId);
        Task<object?> GetPaymentAsync(string paymentId, List<string> pgIds);
        Task<(bool success, object result, int statusCode)> UpdatePaymentAsync(
            string paymentId, string pgId, UpdatePaymentDto dto, string userId);
        Task<(bool success, object result, int statusCode)> SendReceiptAsync(
            string paymentId, string pgId);
        Task<(bool success, object result, int statusCode)> SendReceiptWhatsAppAsync(
            string paymentId, string pgId);
    }
}

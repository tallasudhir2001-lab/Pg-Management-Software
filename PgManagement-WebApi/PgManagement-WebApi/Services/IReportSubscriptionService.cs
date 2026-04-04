using PgManagement_WebApi.DTOs.Settings;

namespace PgManagement_WebApi.Services
{
    public interface IReportSubscriptionService
    {
        List<ReportOptionDto> GetReportOptions();
        Task<List<UserReportSubscriptionDto>> GetReportSubscriptionsAsync(string pgId);
        Task<(bool success, object result, int statusCode)> UpdateReportSubscriptionsAsync(
            string pgId, UpdateReportSubscriptionsRequest request);
    }
}

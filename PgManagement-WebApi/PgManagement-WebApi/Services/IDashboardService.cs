using PgManagement_WebApi.DTOs.Dashboard;
using PgManagement_WebApi.DTOs.Pagination;

namespace PgManagement_WebApi.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetSummaryAsync(List<string> pgIds, DateTime? from, DateTime? to);
        Task<List<RevenueTrendDto>> GetRevenueTrendAsync(List<string> pgIds, DateTime? from, DateTime? to);
        Task<List<RecentPaymentDto>> GetRecentPaymentsAsync(List<string> pgIds, int limit, DateTime? from, DateTime? to);
        Task<object> GetOccupancyAsync(List<string> pgIds);
        Task<DashboardExpenseSummaryDto> GetExpensesSummaryAsync(List<string> pgIds, DateTime? from, DateTime? to);
        Task<DashboardAlertsDto> GetAlertsAsync(List<string> pgIds);
        Task<CollectionSummaryDto> GetCollectionSummaryAsync(List<string> pgIds, DateTime? from, DateTime? to);
        Task<VacancyLossDto> GetVacancyLossAsync(List<string> pgIds);
        Task<TodaySnapshotDto> GetTodaySnapshotAsync(List<string> pgIds);
    }
}

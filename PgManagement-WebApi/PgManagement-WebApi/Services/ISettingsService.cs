using PgManagement_WebApi.DTOs.Settings;

namespace PgManagement_WebApi.Services
{
    public interface ISettingsService
    {
        Task<(bool success, object result, int statusCode)> GetNotificationSettingsAsync(string pgId);
        Task<(bool success, object result, int statusCode)> UpdateNotificationSettingsAsync(string pgId, NotificationSettingsDto dto);
        Task<(bool success, object result, int statusCode)> GetSubscriptionStatusAsync(string pgId);
    }
}

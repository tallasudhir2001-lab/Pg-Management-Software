using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [AccessPoint("Settings", "View Notification Settings")]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var (success, result, statusCode) = await _settingsService.GetNotificationSettingsAsync(pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Settings", "Update Notification Settings")]
        [HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var (success, result, statusCode) = await _settingsService.UpdateNotificationSettingsAsync(pgId, dto);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }

        [AccessPoint("Settings", "View Subscription Status")]
        [HttpGet("subscription-status")]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var (success, result, statusCode) = await _settingsService.GetSubscriptionStatusAsync(pgId);
            if (!success) return StatusCode(statusCode, result);
            return Ok(result);
        }
    }
}

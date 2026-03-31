using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/settings")]
    [ApiController]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/settings/notifications
        [AccessPoint("Settings", "View Notification Settings")]
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null) return NotFound("PG not found.");

            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.PgId == pgId);

            return Ok(new NotificationSettingsResponseDto
            {
                AutoSendPaymentReceipt = settings?.AutoSendPaymentReceipt ?? false,
                SendViaEmail = settings?.SendViaEmail ?? true,
                SendViaWhatsapp = settings?.SendViaWhatsapp ?? false,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            });
        }

        // PUT api/settings/notifications
        [AccessPoint("Settings", "Update Notification Settings")]
        [HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsDto dto)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null) return NotFound("PG not found.");

            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.PgId == pgId);

            if (settings == null)
            {
                settings = new NotificationSettings
                {
                    PgId = pgId,
                    AutoSendPaymentReceipt = dto.AutoSendPaymentReceipt,
                    SendViaEmail = dto.SendViaEmail,
                    SendViaWhatsapp = dto.SendViaWhatsapp
                };
                _context.NotificationSettings.Add(settings);
            }
            else
            {
                settings.AutoSendPaymentReceipt = dto.AutoSendPaymentReceipt;
                settings.SendViaEmail = dto.SendViaEmail;
                settings.SendViaWhatsapp = dto.SendViaWhatsapp;
            }

            await _context.SaveChangesAsync();

            return Ok(new NotificationSettingsResponseDto
            {
                AutoSendPaymentReceipt = settings.AutoSendPaymentReceipt,
                SendViaEmail = settings.SendViaEmail,
                SendViaWhatsapp = settings.SendViaWhatsapp,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            });
        }

        // GET api/settings/subscription-status
        [AccessPoint("Settings", "View Subscription Status")]
        [HttpGet("subscription-status")]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null) return NotFound("PG not found.");

            return Ok(new
            {
                pg.IsEmailSubscriptionEnabled,
                pg.IsWhatsappSubscriptionEnabled
            });
        }
    }
}

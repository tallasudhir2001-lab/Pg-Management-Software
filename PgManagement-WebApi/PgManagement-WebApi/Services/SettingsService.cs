using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;

        public SettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, object result, int statusCode)> GetNotificationSettingsAsync(string pgId)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null)
                return (false, "PG not found.", 404);

            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(ns => ns.PgId == pgId);

            return (true, new NotificationSettingsResponseDto
            {
                AutoSendPaymentReceipt = settings?.AutoSendPaymentReceipt ?? false,
                SendViaEmail = settings?.SendViaEmail ?? true,
                SendViaWhatsapp = settings?.SendViaWhatsapp ?? false,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> UpdateNotificationSettingsAsync(string pgId, NotificationSettingsDto dto)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null)
                return (false, "PG not found.", 404);

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

            return (true, new NotificationSettingsResponseDto
            {
                AutoSendPaymentReceipt = settings.AutoSendPaymentReceipt,
                SendViaEmail = settings.SendViaEmail,
                SendViaWhatsapp = settings.SendViaWhatsapp,
                IsEmailSubscriptionEnabled = pg.IsEmailSubscriptionEnabled,
                IsWhatsappSubscriptionEnabled = pg.IsWhatsappSubscriptionEnabled
            }, 200);
        }

        public async Task<(bool success, object result, int statusCode)> GetSubscriptionStatusAsync(string pgId)
        {
            var pg = await _context.PGs.FindAsync(pgId);
            if (pg == null)
                return (false, "PG not found.", 404);

            return (true, new
            {
                pg.IsEmailSubscriptionEnabled,
                pg.IsWhatsappSubscriptionEnabled
            }, 200);
        }
    }
}

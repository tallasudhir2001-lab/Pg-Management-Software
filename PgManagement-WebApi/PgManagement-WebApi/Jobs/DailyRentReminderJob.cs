using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Jobs
{
    public class DailyRentReminderJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyRentReminderJob> _logger;

        public DailyRentReminderJob(IServiceScopeFactory scopeFactory, ILogger<DailyRentReminderJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
            var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();
            var whatsAppProvider = scope.ServiceProvider.GetRequiredService<IWhatsAppProvider>();

            // Get PGs with either email or whatsapp enabled
            var pgs = await context.PGs
                .Where(pg => pg.IsEmailSubscriptionEnabled || pg.IsWhatsappSubscriptionEnabled)
                .Select(pg => new { pg.PgId, pg.Name, pg.IsEmailSubscriptionEnabled, pg.IsWhatsappSubscriptionEnabled })
                .ToListAsync();

            foreach (var pg in pgs)
            {
                try
                {
                    var settings = await context.NotificationSettings
                        .FirstOrDefaultAsync(ns => ns.PgId == pg.PgId);

                    if (settings == null) continue;

                    var sendEmail = settings.SendViaEmail && pg.IsEmailSubscriptionEnabled;
                    var sendWhatsApp = settings.SendViaWhatsapp && pg.IsWhatsappSubscriptionEnabled;

                    if (!sendEmail && !sendWhatsApp) continue;

                    var asOf = DateTime.Now;
                    var overdueData = await reportService.GetOverdueRentDataAsync(pg.PgId, asOf, null);

                    if (overdueData.Rows.Count == 0) continue;

                    foreach (var row in overdueData.Rows)
                    {
                        if (string.IsNullOrEmpty(row.TenantPhone) || row.TenantPhone.Length < 10)
                            continue;

                        var tenant = await context.Tenants
                            .FirstOrDefaultAsync(t => t.PgId == pg.PgId
                                && t.ContactNumber == row.TenantPhone
                                && !t.isDeleted);

                        if (tenant == null) continue;

                        // Send via Email
                        if (sendEmail && !string.IsNullOrEmpty(tenant.Email))
                        {
                            try
                            {
                                var subject = $"Rent Reminder — {pg.Name}";
                                var html = $"""
                                    <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px;">
                                      <div style="background:#0f1041;color:white;padding:16px 20px;border-radius:12px 12px 0 0;">
                                        <h2 style="margin:0;font-size:18px;">Rent Reminder</h2>
                                        <p style="margin:4px 0 0;opacity:0.8;font-size:13px;">{pg.Name}</p>
                                      </div>
                                      <div style="background:#fff;border:1px solid #e5e7eb;border-top:none;padding:20px;border-radius:0 0 12px 12px;">
                                        <p>Dear {row.TenantName},</p>
                                        <p>This is a friendly reminder that your rent is overdue.</p>
                                        <table style="width:100%;font-size:14px;margin:16px 0;">
                                          <tr><td style="padding:6px 0;color:#6b7280;">Room</td><td style="text-align:right;font-weight:600;">Room {row.RoomNumber}</td></tr>
                                          <tr><td style="padding:6px 0;color:#6b7280;">Paid Up To</td><td style="text-align:right;font-weight:600;">{row.PaidUpTo:dd MMM yyyy}</td></tr>
                                          <tr><td style="padding:6px 0;color:#6b7280;">Days Overdue</td><td style="text-align:right;font-weight:600;color:#991b1b;">{row.DaysOverdue} days</td></tr>
                                          <tr style="border-top:2px solid #f3f4f6;"><td style="padding:6px 0;font-weight:700;">Outstanding</td><td style="text-align:right;font-weight:700;color:#991b1b;">₹{row.OutstandingAmount:N2}</td></tr>
                                        </table>
                                        <p style="color:#6b7280;font-size:13px;">Please make the payment at your earliest convenience. If already paid, kindly ignore this reminder.</p>
                                        <p style="color:#9ca3af;font-size:12px;margin-top:16px;">This is an automated reminder from PG Management Software.</p>
                                      </div>
                                    </div>
                                    """;

                                await emailProvider.SendEmailAsync(tenant.Email, subject, html);
                                _logger.LogInformation("Sent rent reminder email to {TenantName} ({Email}) for PG {PgName}",
                                    row.TenantName, tenant.Email, pg.Name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to send rent reminder email to {Email}", tenant.Email);
                            }
                        }

                        // Send via WhatsApp
                        if (sendWhatsApp)
                        {
                            try
                            {
                                var message = $"🏠 *Rent Reminder — {pg.Name}*\n\n"
                                    + $"Dear {row.TenantName},\n\n"
                                    + $"Your rent is overdue. Here are the details:\n\n"
                                    + $"📍 Room: {row.RoomNumber}\n"
                                    + $"📅 Paid Up To: {row.PaidUpTo:dd MMM yyyy}\n"
                                    + $"⏰ Days Overdue: {row.DaysOverdue} days\n"
                                    + $"💰 Outstanding: ₹{row.OutstandingAmount:N2}\n\n"
                                    + $"Please make the payment at your earliest convenience. If already paid, kindly ignore this reminder.";

                                await whatsAppProvider.SendMessageAsync(tenant.ContactNumber, message);
                                _logger.LogInformation("Sent rent reminder WhatsApp to {TenantName} ({Phone}) for PG {PgName}",
                                    row.TenantName, tenant.ContactNumber, pg.Name);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to send rent reminder WhatsApp to {Phone}", tenant.ContactNumber);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process rent reminders for PG {PgId}", pg.PgId);
                }
            }
        }
    }
}

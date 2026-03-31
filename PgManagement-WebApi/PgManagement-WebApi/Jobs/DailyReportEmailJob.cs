using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi.Jobs
{
    public class DailyReportEmailJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyReportEmailJob> _logger;

        public DailyReportEmailJob(IServiceScopeFactory scopeFactory, ILogger<DailyReportEmailJob> logger)
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

            // Get all PGs with email subscription enabled
            var pgsWithEmail = await context.PGs
                .Where(pg => pg.IsEmailSubscriptionEnabled)
                .Select(pg => new { pg.PgId, pg.Name })
                .ToListAsync();

            foreach (var pg in pgsWithEmail)
            {
                try
                {
                    // Get all report subscriptions for this PG, grouped by user
                    var userSubscriptions = await context.ReportSubscriptions
                        .Where(rs => rs.PgId == pg.PgId && rs.IsEnabled)
                        .Include(rs => rs.User)
                        .GroupBy(rs => new { rs.UserId, rs.User.FullName, rs.User.Email })
                        .Select(g => new
                        {
                            g.Key.UserId,
                            g.Key.FullName,
                            g.Key.Email,
                            ReportTypes = g.Select(rs => rs.ReportType).ToList()
                        })
                        .ToListAsync();

                    foreach (var userSub in userSubscriptions)
                    {
                        if (string.IsNullOrEmpty(userSub.Email)) continue;

                        try
                        {
                            var reportSections = new List<string>();
                            var attachments = new List<(byte[] Data, string FileName)>();
                            var now = DateTime.Now;

                            foreach (var reportType in userSub.ReportTypes)
                            {
                                try
                                {
                                    var (htmlSection, pdfBytes, fileName) = await GenerateReportSection(
                                        reportService, pg.PgId, pg.Name, reportType, now);

                                    if (htmlSection != null)
                                    {
                                        reportSections.Add(htmlSection);
                                        if (pdfBytes != null)
                                            attachments.Add((pdfBytes, fileName));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to generate {ReportType} for PG {PgId}", reportType, pg.PgId);
                                    reportSections.Add($"<p style='color:#991b1b;'>⚠ Could not generate {ReportTypes.All.GetValueOrDefault(reportType).DisplayName ?? reportType} report.</p>");
                                }
                            }

                            if (reportSections.Count == 0) continue;

                            var subject = $"📊 Daily Reports — {pg.Name} — {now:dd MMM yyyy}";
                            var htmlBody = BuildConsolidatedEmail(pg.Name, userSub.FullName ?? "User", reportSections, now);

                            if (attachments.Count > 0)
                            {
                                await emailProvider.SendEmailWithAttachmentsAsync(
                                    userSub.Email, subject, htmlBody, attachments);
                            }
                            else
                            {
                                await emailProvider.SendEmailAsync(userSub.Email, subject, htmlBody);
                            }

                            _logger.LogInformation("Sent daily reports to {Email} for PG {PgName} ({Count} reports)",
                                userSub.Email, pg.Name, reportSections.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send daily reports to {Email} for PG {PgId}",
                                userSub.Email, pg.PgId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process daily reports for PG {PgId}", pg.PgId);
                }
            }
        }

        private async Task<(string? HtmlSection, byte[]? PdfBytes, string FileName)> GenerateReportSection(
            IReportService reportService, string pgId, string pgName, string reportType, DateTime now)
        {
            var month = now.Month;
            var year = now.Year;

            return reportType switch
            {
                ReportTypes.OverdueRent => await BuildOverdueRentSection(reportService, pgId, now),
                ReportTypes.RentCollection => await BuildRentCollectionSection(reportService, pgId, month, year),
                ReportTypes.Occupancy => await BuildOccupancySection(reportService, pgId, now),
                ReportTypes.ProfitLoss => await BuildProfitLossSection(reportService, pgId, month, year),
                ReportTypes.ExpenseSummary => await BuildExpenseSection(reportService, pgId, month, year),
                ReportTypes.AdvanceBalance => await BuildAdvanceBalanceSection(reportService, pgId),
                _ => (null, null, "")
            };
        }

        private async Task<(string?, byte[]?, string)> BuildOverdueRentSection(
            IReportService reportService, string pgId, DateTime asOf)
        {
            var data = await reportService.GetOverdueRentDataAsync(pgId, asOf, null);
            var pdf = await reportService.GenerateOverdueRentReportAsync(pgId, asOf, null);

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#991b1b;margin:0 0 8px 0;font-size:18px;'>⚠ Overdue Rent</h2>";

            if (data.Rows.Count == 0)
            {
                html += "<p style='color:#065f46;'>✅ No overdue rent — all tenants are up to date!</p>";
            }
            else
            {
                html += $"<p style='color:#6b7280;margin:0 0 12px 0;'>{data.Rows.Count} tenant(s) with overdue rent, "
                       + $"total outstanding: <strong>₹{data.TotalOutstanding:N2}</strong></p>";
                html += "<table style='width:100%;border-collapse:collapse;font-size:14px;'>";
                html += "<tr style='background:#fef2f2;'><th style='padding:8px;text-align:left;border-bottom:2px solid #fecaca;'>Tenant</th>"
                       + "<th style='padding:8px;text-align:left;border-bottom:2px solid #fecaca;'>Room</th>"
                       + "<th style='padding:8px;text-align:right;border-bottom:2px solid #fecaca;'>Days Overdue</th>"
                       + "<th style='padding:8px;text-align:right;border-bottom:2px solid #fecaca;'>Outstanding</th></tr>";

                foreach (var row in data.Rows.OrderByDescending(r => r.DaysOverdue))
                {
                    html += $"<tr><td style='padding:8px;border-bottom:1px solid #f3f4f6;'>{row.TenantName}</td>"
                           + $"<td style='padding:8px;border-bottom:1px solid #f3f4f6;'>Room {row.RoomNumber}</td>"
                           + $"<td style='padding:8px;text-align:right;border-bottom:1px solid #f3f4f6;color:#991b1b;font-weight:600;'>{row.DaysOverdue} days</td>"
                           + $"<td style='padding:8px;text-align:right;border-bottom:1px solid #f3f4f6;font-weight:600;'>₹{row.OutstandingAmount:N2}</td></tr>";
                }
                html += "</table>";
            }
            html += "</div>";

            return (html, pdf, "Overdue_Rent.pdf");
        }

        private async Task<(string?, byte[]?, string)> BuildRentCollectionSection(
            IReportService reportService, string pgId, int month, int year)
        {
            var data = await reportService.GetRentCollectionDataAsync(pgId, month, year, null, null);
            var pdf = await reportService.GenerateRentCollectionReportAsync(pgId, month, year, null, null);

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#0f1041;margin:0 0 8px 0;font-size:18px;'>💰 Rent Collection</h2>";
            html += $"<p style='color:#6b7280;margin:0 0 8px 0;'>{new DateTime(year, month, 1):MMMM yyyy}</p>";
            html += "<table style='width:100%;border-collapse:collapse;font-size:14px;'>";
            html += $"<tr><td style='padding:6px 0;'>Expected</td><td style='text-align:right;font-weight:600;'>₹{data.TotalExpected:N2}</td></tr>";
            html += $"<tr><td style='padding:6px 0;'>Collected</td><td style='text-align:right;font-weight:600;color:#065f46;'>₹{data.TotalCollected:N2}</td></tr>";
            html += $"<tr style='border-top:2px solid #e5e7eb;'><td style='padding:6px 0;font-weight:700;'>Pending</td>"
                   + $"<td style='text-align:right;font-weight:700;color:#991b1b;'>₹{data.TotalPending:N2}</td></tr>";
            html += "</table>";

            var unpaid = data.Rows.Where(r => r.Status == "Pending" || r.Status == "Partial").ToList();
            if (unpaid.Count > 0)
            {
                html += $"<p style='color:#92400e;margin:12px 0 4px 0;font-weight:600;'>{unpaid.Count} tenant(s) haven't paid fully:</p>";
                html += "<ul style='margin:0;padding-left:20px;'>";
                foreach (var t in unpaid)
                {
                    html += $"<li style='margin-bottom:4px;'>{t.TenantName} (Room {t.RoomNumber}) — paid ₹{t.AmountPaid:N2} of ₹{t.ExpectedRent:N2}</li>";
                }
                html += "</ul>";
            }
            html += "</div>";

            return (html, pdf, "Rent_Collection.pdf");
        }

        private async Task<(string?, byte[]?, string)> BuildOccupancySection(
            IReportService reportService, string pgId, DateTime asOf)
        {
            var data = await reportService.GetOccupancyDataAsync(pgId, asOf);
            var pdf = await reportService.GenerateOccupancyReportAsync(pgId, asOf);

            var totalBeds = data.Rows.Sum(r => r.TotalBeds);
            var occupied = data.Rows.Sum(r => r.OccupiedBeds);
            var vacant = totalBeds - occupied;
            var pct = totalBeds > 0 ? (occupied * 100.0 / totalBeds) : 0;

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#0f1041;margin:0 0 8px 0;font-size:18px;'>🏠 Occupancy</h2>";
            html += $"<p style='color:#6b7280;margin:0 0 4px 0;'>{occupied}/{totalBeds} beds occupied ({pct:F0}%)</p>";
            if (vacant > 0)
                html += $"<p style='color:#92400e;'>{vacant} bed(s) vacant</p>";
            else
                html += "<p style='color:#065f46;'>🎉 Fully occupied!</p>";
            html += "</div>";

            return (html, pdf, "Occupancy.pdf");
        }

        private async Task<(string?, byte[]?, string)> BuildProfitLossSection(
            IReportService reportService, string pgId, int month, int year)
        {
            var data = await reportService.GetProfitLossDataAsync(pgId, month, year);
            var pdf = await reportService.GenerateProfitLossReportAsync(pgId, month, year);

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#0f1041;margin:0 0 8px 0;font-size:18px;'>📈 Profit & Loss</h2>";
            html += $"<p style='color:#6b7280;margin:0 0 8px 0;'>{new DateTime(year, month, 1):MMMM yyyy}</p>";
            html += "<table style='width:100%;border-collapse:collapse;font-size:14px;'>";
            html += $"<tr><td style='padding:6px 0;'>Total Revenue</td><td style='text-align:right;font-weight:600;color:#065f46;'>₹{data.TotalRevenue:N2}</td></tr>";
            html += $"<tr><td style='padding:6px 0;'>Total Expenses</td><td style='text-align:right;font-weight:600;color:#991b1b;'>₹{data.TotalExpenses:N2}</td></tr>";

            var profitColor = data.NetProfitOrLoss >= 0 ? "#065f46" : "#991b1b";
            html += $"<tr style='border-top:2px solid #e5e7eb;'><td style='padding:6px 0;font-weight:700;'>Net {(data.NetProfitOrLoss >= 0 ? "Profit" : "Loss")}</td>"
                   + $"<td style='text-align:right;font-weight:700;color:{profitColor};'>₹{Math.Abs(data.NetProfitOrLoss):N2}</td></tr>";
            html += "</table></div>";

            return (html, pdf, "Profit_Loss.pdf");
        }

        private async Task<(string?, byte[]?, string)> BuildExpenseSection(
            IReportService reportService, string pgId, int month, int year)
        {
            var data = await reportService.GetExpenseReportDataAsync(pgId, month, year, null);
            var pdf = await reportService.GenerateExpenseReportAsync(pgId, month, year, null);

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#0f1041;margin:0 0 8px 0;font-size:18px;'>💸 Expense Summary</h2>";
            html += $"<p style='color:#6b7280;margin:0 0 8px 0;'>{new DateTime(year, month, 1):MMMM yyyy} — Total: <strong>₹{data.GrandTotal:N2}</strong></p>";

            if (data.Groups.Count > 0)
            {
                html += "<table style='width:100%;border-collapse:collapse;font-size:14px;'>";
                foreach (var cat in data.Groups.OrderByDescending(c => c.Subtotal))
                {
                    html += $"<tr><td style='padding:4px 0;'>{cat.Category}</td>"
                           + $"<td style='text-align:right;font-weight:500;'>₹{cat.Subtotal:N2}</td></tr>";
                }
                html += "</table>";
            }
            html += "</div>";

            return (html, pdf, "Expense_Summary.pdf");
        }

        private async Task<(string?, byte[]?, string)> BuildAdvanceBalanceSection(
            IReportService reportService, string pgId)
        {
            var data = await reportService.GetAdvanceBalanceDataAsync(pgId);
            var pdf = await reportService.GenerateAdvanceBalanceReportAsync(pgId);

            var html = "<div style='margin-bottom:24px;'>";
            html += "<h2 style='color:#0f1041;margin:0 0 8px 0;font-size:18px;'>🏦 Advance Balance</h2>";
            html += $"<p style='color:#6b7280;margin:0 0 4px 0;'>Total held: <strong>₹{data.TotalHeld:N2}</strong> &nbsp;|&nbsp; "
                   + $"Refunded: ₹{data.TotalRefunded:N2} &nbsp;|&nbsp; Net: <strong>₹{data.NetBalance:N2}</strong></p>";
            html += "</div>";

            return (html, pdf, "Advance_Balance.pdf");
        }

        private string BuildConsolidatedEmail(string pgName, string userName, List<string> sections, DateTime now)
        {
            var body = $"""
                <div style="font-family:'Segoe UI',Arial,sans-serif;max-width:640px;margin:0 auto;padding:24px;">
                  <div style="background:#0f1041;color:white;padding:20px 24px;border-radius:12px 12px 0 0;">
                    <h1 style="margin:0;font-size:20px;">📊 Daily Report Summary</h1>
                    <p style="margin:4px 0 0 0;opacity:0.8;font-size:14px;">{pgName} — {now:dd MMMM yyyy}</p>
                  </div>
                  <div style="background:#ffffff;border:1px solid #e5e7eb;border-top:none;padding:24px;border-radius:0 0 12px 12px;">
                    <p style="color:#374151;margin:0 0 20px 0;">Hi {userName},</p>
                    <p style="color:#6b7280;margin:0 0 20px 0;">Here's your daily summary. Detailed PDF reports are attached.</p>
                    {string.Join("\n<hr style='border:none;border-top:1px solid #f3f4f6;margin:16px 0;'/>\n", sections)}
                    <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px 0;"/>
                    <p style="color:#9ca3af;font-size:12px;margin:0;">
                      This is an automated report from PG Management Software.
                      To change your report preferences, visit Configurations → Report Subscriptions.
                    </p>
                  </div>
                </div>
                """;
            return body;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Jobs;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Services
{
    public class ReportSubscriptionService : IReportSubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public ReportSubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ReportOptionDto> GetReportOptions()
        {
            return ReportTypes.All.Select(kv => new ReportOptionDto
            {
                ReportType = kv.Key,
                DisplayName = kv.Value.DisplayName,
                Description = kv.Value.Description
            }).ToList();
        }

        public async Task<List<UserReportSubscriptionDto>> GetReportSubscriptionsAsync(string pgId)
        {
            var userPgs = await _context.UserPgs
                .Where(up => up.PgId == pgId)
                .Include(up => up.User)
                .ToListAsync();

            var subscriptions = await _context.ReportSubscriptions
                .Where(rs => rs.PgId == pgId && rs.IsEnabled)
                .ToListAsync();

            return userPgs.Select(up => new UserReportSubscriptionDto
            {
                UserId = up.UserId,
                FullName = up.User.FullName ?? up.User.UserName ?? "",
                Email = up.User.Email ?? "",
                SubscribedReports = subscriptions
                    .Where(s => s.UserId == up.UserId)
                    .Select(s => s.ReportType)
                    .ToList()
            }).ToList();
        }

        public async Task<(bool success, object result, int statusCode)> UpdateReportSubscriptionsAsync(
            string pgId, UpdateReportSubscriptionsRequest request)
        {
            var validTypes = ReportTypes.All.Keys.ToHashSet();

            foreach (var userSub in request.UserSubscriptions)
            {
                var invalidTypes = userSub.ReportTypes.Where(rt => !validTypes.Contains(rt)).ToList();
                if (invalidTypes.Count > 0)
                    return (false, $"Invalid report types: {string.Join(", ", invalidTypes)}", 400);

                var belongsToPg = await _context.UserPgs
                    .AnyAsync(up => up.UserId == userSub.UserId && up.PgId == pgId);
                if (!belongsToPg)
                    return (false, $"User {userSub.UserId} does not belong to this PG.", 400);
            }

            var existingSubs = await _context.ReportSubscriptions
                .Where(rs => rs.PgId == pgId)
                .ToListAsync();
            _context.ReportSubscriptions.RemoveRange(existingSubs);

            foreach (var userSub in request.UserSubscriptions)
            {
                foreach (var reportType in userSub.ReportTypes)
                {
                    _context.ReportSubscriptions.Add(new ReportSubscription
                    {
                        PgId = pgId,
                        UserId = userSub.UserId,
                        ReportType = reportType,
                        IsEnabled = true
                    });
                }
            }

            await _context.SaveChangesAsync();

            return (true, new { message = "Report subscriptions updated successfully." }, 200);
        }
    }
}

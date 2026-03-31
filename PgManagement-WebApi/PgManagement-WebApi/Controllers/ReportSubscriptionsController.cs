using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PgManagement_WebApi.Attributes;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.DTOs.Settings;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.Jobs;
using PgManagement_WebApi.Models;

namespace PgManagement_WebApi.Controllers
{
    [Route("api/settings/report-subscriptions")]
    [ApiController]
    [Authorize]
    public class ReportSubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportSubscriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get all available report types for subscription.
        /// </summary>
        [AccessPoint("Settings", "View Report Subscriptions")]
        [HttpGet("report-options")]
        public IActionResult GetReportOptions()
        {
            var options = ReportTypes.All.Select(kv => new ReportOptionDto
            {
                ReportType = kv.Key,
                DisplayName = kv.Value.DisplayName,
                Description = kv.Value.Description
            }).ToList();

            return Ok(options);
        }

        /// <summary>
        /// Get all users in this PG with their report subscriptions.
        /// </summary>
        [AccessPoint("Settings", "View Report Subscriptions")]
        [HttpGet]
        public async Task<IActionResult> GetReportSubscriptions()
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            // Get all users for this PG
            var userPgs = await _context.UserPgs
                .Where(up => up.PgId == pgId)
                .Include(up => up.User)
                .ToListAsync();

            // Get all subscriptions for this PG
            var subscriptions = await _context.ReportSubscriptions
                .Where(rs => rs.PgId == pgId && rs.IsEnabled)
                .ToListAsync();

            var result = userPgs.Select(up => new UserReportSubscriptionDto
            {
                UserId = up.UserId,
                FullName = up.User.FullName ?? up.User.UserName ?? "",
                Email = up.User.Email ?? "",
                SubscribedReports = subscriptions
                    .Where(s => s.UserId == up.UserId)
                    .Select(s => s.ReportType)
                    .ToList()
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Update report subscriptions for all users in the PG.
        /// Owner-only: controls which reports are sent to which user.
        /// </summary>
        [AccessPoint("Settings", "Update Report Subscriptions")]
        [HttpPut]
        public async Task<IActionResult> UpdateReportSubscriptions([FromBody] UpdateReportSubscriptionsRequest request)
        {
            var pgId = User.FindFirst("pgId")?.Value;
            if (string.IsNullOrEmpty(pgId)) return Unauthorized();

            // Validate report types
            var validTypes = ReportTypes.All.Keys.ToHashSet();

            foreach (var userSub in request.UserSubscriptions)
            {
                var invalidTypes = userSub.ReportTypes.Where(rt => !validTypes.Contains(rt)).ToList();
                if (invalidTypes.Count > 0)
                    return BadRequest($"Invalid report types: {string.Join(", ", invalidTypes)}");

                // Verify user belongs to this PG
                var belongsToPg = await _context.UserPgs
                    .AnyAsync(up => up.UserId == userSub.UserId && up.PgId == pgId);
                if (!belongsToPg)
                    return BadRequest($"User {userSub.UserId} does not belong to this PG.");
            }

            // Remove existing subscriptions for this PG
            var existingSubs = await _context.ReportSubscriptions
                .Where(rs => rs.PgId == pgId)
                .ToListAsync();
            _context.ReportSubscriptions.RemoveRange(existingSubs);

            // Add new subscriptions
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

            return Ok(new { message = "Report subscriptions updated successfully." });
        }
    }
}

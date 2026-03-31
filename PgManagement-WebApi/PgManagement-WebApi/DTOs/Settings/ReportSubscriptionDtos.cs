namespace PgManagement_WebApi.DTOs.Settings
{
    public class ReportOptionDto
    {
        public string ReportType { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
    }

    public class UserReportSubscriptionDto
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public List<string> SubscribedReports { get; set; } = new();
    }

    public class UpdateUserReportSubscriptionsDto
    {
        public string UserId { get; set; }
        public List<string> ReportTypes { get; set; } = new();
    }

    public class UpdateReportSubscriptionsRequest
    {
        public List<UpdateUserReportSubscriptionsDto> UserSubscriptions { get; set; } = new();
    }
}

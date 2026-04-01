namespace PgManagement_WebApi.DTOs.Audit
{
    public class AuditEventListDto
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string? Description { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string PerformedBy { get; set; }
        public DateTime PerformedAt { get; set; }
        public bool IsReviewed { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class AuditCountDto
    {
        public int UnreviewedCount { get; set; }
    }
}

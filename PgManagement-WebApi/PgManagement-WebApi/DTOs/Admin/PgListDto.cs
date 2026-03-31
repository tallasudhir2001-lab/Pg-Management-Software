namespace PgManagement_WebApi.DTOs.Admin
{
    public class PgListDto
    {
        public string PgId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public int UserCount { get; set; }
        public string? BranchId { get; set; }
        public string? BranchName { get; set; }
        public bool IsEmailSubscriptionEnabled { get; set; }
        public bool IsWhatsappSubscriptionEnabled { get; set; }
    }

    public class BranchDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int PgCount { get; set; }
        public List<BranchPgDto> PGs { get; set; } = new();
    }

    public class BranchPgDto
    {
        public string PgId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

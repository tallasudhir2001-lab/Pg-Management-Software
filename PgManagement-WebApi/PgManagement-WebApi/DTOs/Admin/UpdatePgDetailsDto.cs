namespace PgManagement_WebApi.DTOs.Admin
{
    public class UpdatePgDetailsDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string? OwnerEmail { get; set; }
    }
}

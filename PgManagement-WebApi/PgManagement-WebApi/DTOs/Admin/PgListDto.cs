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
    }
}

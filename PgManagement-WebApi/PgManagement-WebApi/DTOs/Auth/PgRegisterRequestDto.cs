namespace PgManagement_WebApi.DTOs.Auth
{
    public class PgRegisterRequestDto
    {
        public string PgName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// If provided, links the new PG to this existing branch.
        /// If null, a new branch is auto-created.
        /// </summary>
        public string? BranchId { get; set; }
        /// <summary>
        /// Name for the new branch. Used only when BranchId is null.
        /// </summary>
        public string? BranchName { get; set; }
    }
}

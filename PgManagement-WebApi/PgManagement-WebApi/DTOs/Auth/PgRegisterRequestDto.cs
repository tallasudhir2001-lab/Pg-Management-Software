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
    }
}

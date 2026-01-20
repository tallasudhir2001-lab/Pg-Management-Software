namespace PgManagement_WebApi.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}

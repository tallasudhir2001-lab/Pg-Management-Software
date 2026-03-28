namespace PgManagement_WebApi.DTOs.Auth
{
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequestDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

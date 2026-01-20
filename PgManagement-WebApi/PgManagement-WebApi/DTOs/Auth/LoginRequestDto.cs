namespace PgManagement_WebApi.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string UserNameOrEmail { get; set; }
        public string Password { get; set; }
    }
}

namespace PgManagement_WebApi.DTOs.PgUser
{
    public class PgUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class AddPgUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateUserRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
    }
}

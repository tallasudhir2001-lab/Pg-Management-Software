namespace PgManagement_WebApi.DTOs.PgUser
{
    public class AssignedPgDto
    {
        public string PgId { get; set; } = string.Empty;
        public string PgName { get; set; } = string.Empty;
    }

    public class PgUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<AssignedPgDto> AssignedPgs { get; set; } = new();
    }

    public class AddPgUserDto
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        /// <summary>Required only when creating a new user. Ignored if user already exists.</summary>
        public string? Password { get; set; }
        public string RoleName { get; set; } = string.Empty;
        /// <summary>Which PGs (within the branch) to assign this user to. Empty = current PG only.</summary>
        public List<string> PgIds { get; set; } = new();
    }

    public class UpdateUserRoleDto
    {
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateUserPgsDto
    {
        public List<string> PgIds { get; set; } = new();
    }

    public class BranchPgInfoDto
    {
        public string PgId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

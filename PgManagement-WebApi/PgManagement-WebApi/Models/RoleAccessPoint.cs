using Microsoft.AspNetCore.Identity;

namespace PgManagement_WebApi.Models
{
    public class RoleAccessPoint
    {
        public string RoleId { get; set; } = string.Empty;
        public IdentityRole Role { get; set; } = null!;

        public int AccessPointId { get; set; }
        public AccessPoint AccessPoint { get; set; } = null!;
    }
}

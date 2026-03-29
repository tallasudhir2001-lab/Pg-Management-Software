using Microsoft.AspNetCore.Identity;

namespace PgManagement_WebApi.Identity
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>Human-readable display name (e.g. "John Doe"). Separate from UserName which is the login identifier.</summary>
        public string? FullName { get; set; }
    }
}

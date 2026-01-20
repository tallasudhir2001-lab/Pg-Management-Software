using PgManagement_WebApi.Identity;
using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class UserPg
    {
        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        [Required]
        public int RoleId { get; set; }
        public PgRole Role { get; set; }
    }
}

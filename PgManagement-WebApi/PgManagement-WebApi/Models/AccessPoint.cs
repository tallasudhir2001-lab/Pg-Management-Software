using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class AccessPoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Module { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;

        [MaxLength(300)]
        public string Route { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<RoleAccessPoint> RoleAccessPoints { get; set; } = new List<RoleAccessPoint>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class PgRole
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class EmployeeRole
    {
        [Key]
        [MaxLength(30)]
        public string Code { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }
}

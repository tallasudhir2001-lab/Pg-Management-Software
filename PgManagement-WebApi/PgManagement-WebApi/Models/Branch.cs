using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class Branch
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PG> PGs { get; set; } = new List<PG>();
    }
}

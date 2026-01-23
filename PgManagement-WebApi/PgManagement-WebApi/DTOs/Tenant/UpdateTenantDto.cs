using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.DTOs.Tenant
{
    public class UpdateTenantDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string ContactNumber { get; set; }
        [Required]
        public string AadharNumber { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public DateTime? RentPaidUpto { get; set; }
        public string Notes { get; set; }
    }
}

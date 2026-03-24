using PgManagement_WebApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PgManagement_WebApi.DTOs.Tenant
{
    public class CreateTenantDto
    {
        public string Name { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        [Required]
        public string RoomId { get; set; }       
        public string ContactNumber { get; set; }
        public string AadharNumber { get; set; }
        public bool HasAdvance { get; set; } 
        public decimal? AdvanceAmount { get; set; }
        public string? PaymentModeCode { get; set; }
        public string Notes { get; set; }
    }
}

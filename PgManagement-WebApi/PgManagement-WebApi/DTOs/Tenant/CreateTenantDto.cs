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
        public string RoomId { get; set; }         // initial room
        public string ContactNumber { get; set; }
        public string AadharNumber { get; set; }
        public decimal? AdvanceAmount { get; set; }
        public DateTime? RentPaidUpto { get; set; }
        public string Notes { get; set; }
    }
}

using PgManagement_WebApi.DTOs.Payment.PendingRent;

namespace PgManagement_WebApi.DTOs.Payment
{
    public class PaymentContextDto
    {
        public string TenantId { get; set; }
        public string TenantName { get; set; }

        public DateTime? PaidFrom { get; set; }     
        public DateTime? MaxPaidUpto { get; set; }  
        public decimal PendingAmount { get; set; }
        public DateTime AsOfDate { get; set; }

        public bool HasActiveStay { get; set; }
        public List<PendingStayContextDto> PendingStays { get; set; } = new();

        public string? RoomNumber { get; set; }
        public decimal RentPerMonth { get; set; }
        public string StayType { get; set; } = "MONTHLY";
        public DateTime? StayStartDate { get; set; }
    }

}

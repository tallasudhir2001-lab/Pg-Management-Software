namespace PgManagement_WebApi.DTOs.Payment
{
    public class PaymentContextDto
    {
        public string TenantId { get; set; }
        public string TenantName { get; set; }

        public DateTime PaidFrom { get; set; }     
        public DateTime? MaxPaidUpto { get; set; }  
        public decimal PendingAmount { get; set; }
        public DateTime AsOfDate { get; set; }

        public bool HasActiveStay { get; set; }
    }

}

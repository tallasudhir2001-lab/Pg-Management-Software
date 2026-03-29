namespace PgManagement_WebApi.DTOs.Payment.PendingRent
{
    public class PendingStayContextDto
    {
        public string RoomId { get; set; }
        public string RoomNumber { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal PendingAmount { get; set; }

        public bool IsActiveStay { get; set; }
        public bool IsNextPayable { get; set; }
        public decimal RentPerMonth { get; set; }
    }
}

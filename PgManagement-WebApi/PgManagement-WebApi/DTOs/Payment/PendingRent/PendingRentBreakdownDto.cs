namespace PgManagement_WebApi.DTOs.Payment.PendingRent
{
    public class PendingRentBreakdownDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal RentPerDay { get; set; }
        public decimal Amount { get; set; }
        public string RoomNumber { get; set; }
    }
}

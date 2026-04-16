namespace PgManagement_WebApi.DTOs.Reports
{
    public class BookingConversionReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalBookings { get; set; }
        public int CheckedIn { get; set; }
        public int Cancelled { get; set; }
        public int Expired { get; set; }
        public int StillActive { get; set; }
        public double ConversionRatePercent { get; set; }
    }
}

namespace PgManagement_WebApi.Models.NonEntityModels
{
    public class RentSlice
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal RentPerDay { get; set; }
        public decimal Amount { get; set; }
    }
}

namespace PgManagement_WebApi.DTOs.Admin
{
    public class UpdatePgSubscriptionDto
    {
        public bool IsEmailSubscriptionEnabled { get; set; }
        public bool IsWhatsappSubscriptionEnabled { get; set; }
    }

    public class PgSubscriptionDto
    {
        public string PgId { get; set; }
        public string PgName { get; set; }
        public bool IsEmailSubscriptionEnabled { get; set; }
        public bool IsWhatsappSubscriptionEnabled { get; set; }
    }
}

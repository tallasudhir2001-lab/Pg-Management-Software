namespace PgManagement_WebApi.DTOs.Tenant
{
    public class ChangeStayTypeDto
    {
        public string NewStayType { get; set; } = "MONTHLY";
        public DateTime EffectiveDate { get; set; }
    }
}

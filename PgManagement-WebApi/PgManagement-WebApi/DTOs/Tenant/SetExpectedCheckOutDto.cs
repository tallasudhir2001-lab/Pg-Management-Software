namespace PgManagement_WebApi.DTOs.Tenant
{
    public class SetExpectedCheckOutDto
    {
        /// <summary>
        /// The expected checkout date. Set to null to clear.
        /// </summary>
        public DateTime? ExpectedCheckOutDate { get; set; }
    }
}

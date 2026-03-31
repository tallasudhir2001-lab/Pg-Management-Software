using System.ComponentModel.DataAnnotations;

namespace PgManagement_WebApi.Models
{
    public class NotificationSettings
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string PgId { get; set; }
        public PG PG { get; set; }

        /// <summary>
        /// Auto-send payment receipt to tenant upon payment creation
        /// </summary>
        public bool AutoSendPaymentReceipt { get; set; }

        /// <summary>
        /// Send receipt via email (requires email subscription on PG)
        /// </summary>
        public bool SendViaEmail { get; set; } = true;

        /// <summary>
        /// Send receipt via WhatsApp (requires WhatsApp subscription on PG)
        /// </summary>
        public bool SendViaWhatsapp { get; set; }
    }
}

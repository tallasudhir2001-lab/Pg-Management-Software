namespace PgManagement_WebApi.Services
{
    public interface IEmailProvider
    {
        Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachment = null,
            string? attachmentName = null);

        Task SendEmailWithAttachmentsAsync(
            string to,
            string subject,
            string htmlBody,
            List<(byte[] Data, string FileName)> attachments);
    }
}

namespace PgManagement_WebApi.Services
{
    public interface IWhatsAppProvider
    {
        Task SendMessageAsync(string phoneNumber, string message);
        Task SendDocumentAsync(string phoneNumber, byte[] document, string fileName, string caption);
    }
}

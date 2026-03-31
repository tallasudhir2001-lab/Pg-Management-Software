using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PgManagement_WebApi.Options;

namespace PgManagement_WebApi.Services
{
    public class SmtpEmailProvider : IEmailProvider
    {
        private readonly EmailOptions _emailOptions;

        public SmtpEmailProvider(IOptions<EmailOptions> emailOptions)
        {
            _emailOptions = emailOptions.Value;
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            byte[]? attachment = null,
            string? attachmentName = null)
        {
            var smtp = _emailOptions.Smtp ?? new SmtpOptions();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };

            if (attachment != null && attachmentName != null)
                bodyBuilder.Attachments.Add(attachmentName, attachment, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port,
                smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(smtp.Username))
                await client.AuthenticateAsync(smtp.Username, smtp.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendEmailWithAttachmentsAsync(
            string to,
            string subject,
            string htmlBody,
            List<(byte[] Data, string FileName)> attachments)
        {
            var smtp = _emailOptions.Smtp ?? new SmtpOptions();
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            foreach (var (data, fileName) in attachments)
                bodyBuilder.Attachments.Add(fileName, data, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp.Host, smtp.Port,
                smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(smtp.Username))
                await client.AuthenticateAsync(smtp.Username, smtp.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Options;
using MimeKit;
using PgManagement_WebApi.Options;

namespace PgManagement_WebApi.Services
{
    public class AwsSesEmailProvider : IEmailProvider
    {
        private readonly EmailOptions _emailOptions;

        public AwsSesEmailProvider(IOptions<EmailOptions> emailOptions)
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
            var ses = _emailOptions.AwsSes ?? new AwsSesOptions();

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailOptions.FromName, _emailOptions.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            if (attachment != null && attachmentName != null)
                bodyBuilder.Attachments.Add(attachmentName, attachment, ContentType.Parse("application/pdf"));

            message.Body = bodyBuilder.ToMessageBody();

            using var memoryStream = new MemoryStream();
            await message.WriteToAsync(memoryStream);
            memoryStream.Position = 0;

            var client = new AmazonSimpleEmailServiceClient(
                ses.AccessKey,
                ses.SecretKey,
                RegionEndpoint.GetBySystemName(ses.Region));

            await client.SendRawEmailAsync(new SendRawEmailRequest
            {
                RawMessage = new RawMessage { Data = memoryStream }
            });
        }
    }
}

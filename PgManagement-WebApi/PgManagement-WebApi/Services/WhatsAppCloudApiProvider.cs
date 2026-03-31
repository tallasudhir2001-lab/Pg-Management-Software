using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PgManagement_WebApi.Options;

namespace PgManagement_WebApi.Services
{
    public class WhatsAppCloudApiProvider : IWhatsAppProvider
    {
        private readonly WhatsAppOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppCloudApiProvider> _logger;

        public WhatsAppCloudApiProvider(
            IOptions<WhatsAppOptions> options,
            HttpClient httpClient,
            ILogger<WhatsAppCloudApiProvider> logger)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendMessageAsync(string phoneNumber, string message)
        {
            var phone = NormalizePhone(phoneNumber);
            var url = $"https://graph.facebook.com/v21.0/{_options.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "text",
                text = new { body = message }
            };

            var request = CreateRequest(HttpMethod.Post, url, payload);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("WhatsApp send failed: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"WhatsApp API error: {response.StatusCode}");
            }

            _logger.LogInformation("WhatsApp message sent to {Phone}", phone);
        }

        public async Task SendDocumentAsync(string phoneNumber, byte[] document, string fileName, string caption)
        {
            // Step 1: Upload the media
            var mediaId = await UploadMediaAsync(document, fileName);

            // Step 2: Send the document message
            var phone = NormalizePhone(phoneNumber);
            var url = $"https://graph.facebook.com/v21.0/{_options.PhoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = phone,
                type = "document",
                document = new
                {
                    id = mediaId,
                    filename = fileName,
                    caption
                }
            };

            var request = CreateRequest(HttpMethod.Post, url, payload);
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("WhatsApp document send failed: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"WhatsApp API error: {response.StatusCode}");
            }

            _logger.LogInformation("WhatsApp document sent to {Phone}: {FileName}", phone, fileName);
        }

        private async Task<string> UploadMediaAsync(byte[] data, string fileName)
        {
            var url = $"https://graph.facebook.com/v21.0/{_options.PhoneNumberId}/media";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("whatsapp"), "messaging_product");
            content.Add(new StringContent("application/pdf"), "type");

            var fileContent = new ByteArrayContent(data);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", fileName);

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WhatsApp media upload failed: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"WhatsApp media upload error: {response.StatusCode}");
            }

            var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("No media ID returned");
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            return request;
        }

        private string NormalizePhone(string phone)
        {
            // Strip non-digits
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // If 10 digits (Indian number), prepend country code
            if (digits.Length == 10)
                digits = "91" + digits;

            return digits;
        }
    }
}

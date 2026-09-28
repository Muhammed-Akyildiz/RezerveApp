using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace RezerveApp.Services
{
    // WhatsApp Business Cloud API (Meta Graph API) entegrasyonu.
    // Gerekli ayarlar appsettings.json -> "WhatsApp" bölümünden okunur:
    //   WhatsApp:Enabled        -> true/false (false ise gerçek istek atılmaz)
    //   WhatsApp:ApiBaseUrl     -> https://graph.facebook.com
    //   WhatsApp:ApiVersion     -> ör. v20.0
    //   WhatsApp:PhoneNumberId  -> Meta'dan alınan gönderen numara ID'si
    //   WhatsApp:AccessToken    -> Meta'dan alınan kalıcı/geçici erişim anahtarı
    // Bu değerler appsettings.json, appsettings.Production.json veya ortam
    // değişkenleri üzerinden ayarlanmalı; gerçek anahtar asla kod içine
    // gömülmemelidir.
    public class WhatsAppBusinessCloudService : IWhatsAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public WhatsAppBusinessCloudService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<WhatsAppSendResult> SendMessageAsync(string toPhoneE164, string message)
        {
            var enabled = _configuration.GetValue<bool>("WhatsApp:Enabled");

            if (!enabled)
            {
                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = "WhatsApp entegrasyonu yapılandırılmadı (WhatsApp:Enabled=false). " +
                        "appsettings.json içinde WhatsApp:PhoneNumberId ve WhatsApp:AccessToken " +
                        "değerlerini girip WhatsApp:Enabled'i true yapın."
                };
            }

            var phoneNumberId = _configuration["WhatsApp:PhoneNumberId"];
            var accessToken = _configuration["WhatsApp:AccessToken"];
            var apiBaseUrl = _configuration["WhatsApp:ApiBaseUrl"] ?? "https://graph.facebook.com";
            var apiVersion = _configuration["WhatsApp:ApiVersion"] ?? "v20.0";

            if (string.IsNullOrWhiteSpace(phoneNumberId) || string.IsNullOrWhiteSpace(accessToken))
            {
                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = "WhatsApp:PhoneNumberId veya WhatsApp:AccessToken eksik."
                };
            }

            var url = $"{apiBaseUrl.TrimEnd('/')}/{apiVersion}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                to = toPhoneE164,
                type = "text",
                text = new { preview_url = false, body = message }
            };

            var requestJson = JsonSerializer.Serialize(payload);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    string? messageId = null;

                    try
                    {
                        using var doc = JsonDocument.Parse(responseBody);
                        if (doc.RootElement.TryGetProperty("messages", out var messages) &&
                            messages.GetArrayLength() > 0 &&
                            messages[0].TryGetProperty("id", out var idProp))
                        {
                            messageId = idProp.GetString();
                        }
                    }
                    catch (JsonException)
                    {
                        // Yanıt beklenenden farklıysa mesaj ID'sini atla, yine de başarı say.
                    }

                    return new WhatsAppSendResult { Success = true, ProviderMessageId = messageId };
                }

                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = $"WhatsApp API hatası ({(int)response.StatusCode}): {responseBody}"
                };
            }
            catch (HttpRequestException ex)
            {
                return new WhatsAppSendResult
                {
                    Success = false,
                    ErrorMessage = $"WhatsApp API'ye ulaşılamadı: {ex.Message}"
                };
            }
        }
    }
}

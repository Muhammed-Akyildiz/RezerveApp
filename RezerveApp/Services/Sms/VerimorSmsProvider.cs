using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RezerveApp.Services.Sms
{
    public class VerimorSmsProvider : ISmsProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VerimorSmsProvider> _logger;
        private readonly HttpClient _httpClient;

        public VerimorSmsProvider(IConfiguration configuration, ILogger<VerimorSmsProvider> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var username = _configuration["Sms:Verimor:Username"];
                var password = _configuration["Sms:Verimor:Password"];
                var senderId = _configuration["Sms:Verimor:SenderId"];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(senderId))
                {
                    _logger.LogError("Verimor SMS credentials are not fully configured.");
                    return false;
                }

                // Normalize phone number (ensure 12 digits, e.g., 905xxxxxxxxx)
                var normalizedPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
                if (normalizedPhone.StartsWith("0"))
                {
                    normalizedPhone = "9" + normalizedPhone;
                }
                else if (normalizedPhone.Length == 10)
                {
                    normalizedPhone = "90" + normalizedPhone;
                }

                var payload = new
                {
                    username = username,
                    password = password,
                    source_addr = senderId,
                    messages = new[]
                    {
                        new { msg = message, dest = normalizedPhone }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("https://sms.verimor.com.tr/v2/send.json", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SMS sent successfully to {PhoneNumber}. Response: {Response}", normalizedPhone, responseContent);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send SMS to {PhoneNumber}. Status Code: {StatusCode}. Response: {Response}", 
                        normalizedPhone, response.StatusCode, responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending SMS to {PhoneNumber}", phoneNumber);
                return false;
            }
        }
    }
}

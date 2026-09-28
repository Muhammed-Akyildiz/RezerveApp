using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace RezerveApp.Services.Sms
{
    public class NetgsmSmsProvider : ISmsProvider
    {
        private readonly ILogger<NetgsmSmsProvider> _logger;

        public NetgsmSmsProvider(ILogger<NetgsmSmsProvider> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            // TODO: NETGSM API Entegrasyonu buraya gelecek.
            // Örnek: HttpClient ile https://api.netgsm.com.tr/sms/send/get adresine istek atılacak.

            _logger.LogInformation("\n================== SMS SENT ==================");
            _logger.LogInformation($"To: {phoneNumber}");
            _logger.LogInformation($"Message: {message}");
            _logger.LogInformation("==============================================\n");

            // Şimdilik başarılı kabul edip logluyoruz.
            return Task.FromResult(true);
        }
    }
}

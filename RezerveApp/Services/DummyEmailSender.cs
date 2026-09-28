using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RezerveApp.Services
{
    public class DummyEmailSender : IEmailSender
    {
        private readonly ILogger<DummyEmailSender> _logger;

        public DummyEmailSender(ILogger<DummyEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Gerçek bir SMTP servisine bağlanana kadar konsola yazdırır.
            _logger.LogInformation("================== EMAIL GÖNDERİMİ ==================");
            _logger.LogInformation($"KİME: {email}");
            _logger.LogInformation($"KONU: {subject}");
            _logger.LogInformation($"İÇERİK:\n{htmlMessage}");
            _logger.LogInformation("=====================================================");

            return Task.CompletedTask;
        }
    }
}

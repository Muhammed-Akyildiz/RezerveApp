using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace RezerveApp.Services
{
    public class BrevoEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BrevoEmailSender> _logger;

        public BrevoEmailSender(IConfiguration configuration, ILogger<BrevoEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"];
                var port = int.Parse(_configuration["Email:Port"] ?? "587");
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:BrevoKey1"] + _configuration["Email:BrevoKey2"];
                if (string.IsNullOrEmpty(password) || password.Length < 10) 
                {
                    password = _configuration["Email:Password"]; // Fallback
                }
                var senderEmail = _configuration["Email:SenderEmail"] ?? "info@rezerveapp.com.tr";
                var senderName = _configuration["Email:SenderName"] ?? "RezerveApp";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlMessage };
                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                // Brevo uses STARTTLS on port 587
                await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", email);
            }
        }
    }
}

using System.Threading.Tasks;

namespace RezerveApp.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}

using System.Threading.Tasks;

namespace RezerveApp.Services
{
    public interface IOtpService
    {
        Task<bool> GenerateAndSendOtpAsync(string phoneNumber, string purpose);
        Task<bool> VerifyOtpAsync(string phoneNumber, string code, string purpose);
    }
}

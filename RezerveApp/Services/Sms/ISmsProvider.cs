namespace RezerveApp.Services.Sms
{
    public interface ISmsProvider
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }
}

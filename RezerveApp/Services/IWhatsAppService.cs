namespace RezerveApp.Services
{
    public class WhatsAppSendResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ProviderMessageId { get; set; }
    }

    // WhatsApp mesajı gönderen servisin sözleşmesi. Gerçek sağlayıcı
    // (WhatsApp Business Cloud API vb.) bu arayüzü uygular; böylece
    // ileride sağlayıcı değiştirilirse çağıran kodun değişmesi gerekmez.
    public interface IWhatsAppService
    {
        Task<WhatsAppSendResult> SendMessageAsync(string toPhoneE164, string message);
    }
}

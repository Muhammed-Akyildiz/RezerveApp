namespace RezerveApp.Models
{
    // Gönderilmeye çalışılan her WhatsApp bildiriminin kaydı.
    // Entegrasyon yapılandırılmamışsa bile burada IsSuccess=false ve
    // ErrorMessage ile birlikte kaydedilir; sessizce yutulmaz.
    public class NotificationLog
    {
        public int Id { get; set; }

        public int? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        public NotificationType Type { get; set; }

        public string RecipientPhone { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

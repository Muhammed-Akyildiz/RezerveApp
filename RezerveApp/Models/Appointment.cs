using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string CustomerPhone { get; set; } = string.Empty;

        public string? CustomerEmail { get; set; }

        // Faz 6 (müşteri yönetimi) ile ilişkilendirilir; eski kayıtlarla
        // geriye dönük uyum için nullable bırakıldı.
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }

        public DateTime AppointmentDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Status { get; set; } = "Pending";

        // 30 dakika önce hatırlatma gönderildi mi? (WhatsApp hosted service)
        public bool ReminderSent { get; set; } = false;

        // Concurrency token: iki eşzamanlı isteğin aynı slotu
        // aynı anda onaylamasını (double-booking) engellemek için.
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
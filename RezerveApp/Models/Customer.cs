using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    // İşletmeye ait müşteri kaydı. Aynı telefonla tekrar gelen
    // müşteri, randevu oluşturulurken bu kayıtla eşleştirilir.
    public class Customer
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string Phone { get; set; } = string.Empty;

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int NoShowCount { get; set; } = 0;
        public bool IsBlacklisted { get; set; } = false;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}

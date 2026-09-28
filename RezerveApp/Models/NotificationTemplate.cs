using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class NotificationTemplate
    {
        public int Id { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(10)]
        public string Language { get; set; } = "tr"; // Varsayılan Türkçe

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}

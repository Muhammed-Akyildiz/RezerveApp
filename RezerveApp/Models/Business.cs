using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class Business
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        [StringLength(11, MinimumLength = 11)]
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }

        public string? Email { get; set; }
        public string? Instagram { get; set; }

        // Adres - cascade select (Faz 7'de kullanılacak)
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Neighborhood { get; set; }

        // Harita konumu (Faz 7'de kullanılacak)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // SuperAdmin tarafından yönetilen onay/durum bilgisi
        public BusinessStatus Status { get; set; } = BusinessStatus.Pending;

        // Aktif/pasif anahtarı - pasif işletmeler müşteri sayfasında
        // ve randevu akışında görünmez/kullanılamaz olmalı
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Randevu periyodu (Örn: 20, 30, 45 dk)
        public int SlotInterval { get; set; } = 30;

        public string? GalleryImage1 { get; set; }
        public string? GalleryImage2 { get; set; }
        public string? GalleryImage3 { get; set; }

        public ICollection<BusinessSubscription> Subscriptions { get; set; } = new List<BusinessSubscription>();
    }
}

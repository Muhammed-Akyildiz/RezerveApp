using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    // Sistemdeki abonelik paketi kataloğu (ör. Basic, Premium).
    // Fiyat/limit gibi kurallar burada tanımlanır; işletmeler
    // BusinessSubscription üzerinden bir plana bağlanır.
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public SubscriptionPlanType Type { get; set; }

        // null = sınırsız çalışan (Premium)
        public int? MaxEmployees { get; set; }
        
        // null = sınırsız hizmet
        public int? MaxServices { get; set; }

        [Range(0, 1000000)]
        public decimal MonthlyPrice { get; set; }

        [Range(0, 1000000)]
        public decimal YearlyPrice { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

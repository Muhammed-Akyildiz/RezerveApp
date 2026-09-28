namespace RezerveApp.Models
{
    // Bir işletmenin o anki (veya geçmiş) abonelik kaydı.
    // Business - SubscriptionPlan arasındaki ilişkiyi ve
    // faturalama periyodunu / geçerlilik tarihlerini tutar.
    public class BusinessSubscription
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        public BillingPeriod BillingPeriod { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        // İşletmenin şu anda geçerli olan aboneliği mi?
        // Bir işletmenin aynı anda yalnızca bir aktif aboneliği olmalı.
        public bool IsActive { get; set; } = true;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public DateTime? PaymentDate { get; set; }
    }
}

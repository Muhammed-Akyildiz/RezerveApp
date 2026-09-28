using RezerveApp.Data;
using RezerveApp.Models;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Services
{
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // AKTİF ABONELİĞİ GETİR
        // =========================================================
        public async Task<BusinessSubscription?> GetActiveSubscriptionAsync(int businessId)
        {
            return await _context.BusinessSubscriptions
                .Include(bs => bs.SubscriptionPlan)
                .Where(bs =>
                    bs.BusinessId == businessId &&
                    bs.IsActive)
                .OrderByDescending(bs => bs.StartDate)
                .FirstOrDefaultAsync();
        }


        // =========================================================
        // PAKET ATA
        // =========================================================
        public async Task<BusinessSubscription> AssignPlanAsync(
            int businessId,
            int subscriptionPlanId,
            BillingPeriod billingPeriod)
        {
            var currentActiveSubscriptions =
                await _context.BusinessSubscriptions
                    .Where(bs =>
                        bs.BusinessId == businessId &&
                        bs.IsActive)
                    .ToListAsync();


            // Eski aktif abonelikleri kapat
            foreach (var subscription in currentActiveSubscriptions)
            {
                subscription.IsActive = false;
                subscription.EndDate = DateTime.UtcNow;
            }


            // Yeni abonelik oluştur
            var newSubscription = new BusinessSubscription
            {
                BusinessId = businessId,
                SubscriptionPlanId = subscriptionPlanId,
                BillingPeriod = billingPeriod,
                StartDate = DateTime.UtcNow,
                IsActive = true
            };


            _context.BusinessSubscriptions.Add(newSubscription);

            await _context.SaveChangesAsync();

            return newSubscription;
        }


        // =========================================================
        // ÇALIŞAN EKLENEBİLİR Mİ?
        // =========================================================
        
        // =========================================================
        // HİZMET EKLENEBİLİR Mİ?
        // =========================================================
        public async Task<(bool CanAdd, int? MaxServices, int CurrentCount)>
            CanAddServiceAsync(int businessId)
        {
            var activeSubscription = await GetActiveSubscriptionAsync(businessId);
            
            int? maxServices;
            
            if (activeSubscription == null)
            {
                // Varsayılan paket: Başlangıç (5 Hizmet)
                maxServices = 5;
            }
            else
            {
                maxServices = activeSubscription.SubscriptionPlan?.MaxServices;
            }
            
            var currentCount = await _context.Services.CountAsync(s => s.BusinessId == businessId);
            
            if (maxServices == null) return (true, null, currentCount);
            
            return (currentCount < maxServices, maxServices, currentCount);
        }

        public async Task<(bool CanAdd, int? MaxEmployees, int CurrentCount)>
            CanAddEmployeeAsync(int businessId)
        {
            var activeSubscription =
                await GetActiveSubscriptionAsync(businessId);


            /*
             * Aktif abonelik yoksa Basic kabul ediyoruz.
             *
             * Basic:
             * MaxEmployees = 2
             *
             * Premium:
             * MaxEmployees = null
             *
             * null = SINIRSIZ
             */

            int? maxEmployees;


            if (activeSubscription == null)
            {
                // Varsayılan paket: Başlangıç
                maxEmployees = 1;
            }
            else
            {
                maxEmployees =
                    activeSubscription.SubscriptionPlan?.MaxEmployees;
            }


            // Sadece AKTİF çalışanları say
            var currentCount =
                await _context.Employees
                    .CountAsync(e =>
                        e.BusinessId == businessId &&
                        e.IsActive);


            // =====================================================
            // SINIRSIZ PAKET
            // =====================================================

            if (maxEmployees == null)
            {
                return (
                    CanAdd: true,
                    MaxEmployees: null,
                    CurrentCount: currentCount
                );
            }


            // =====================================================
            // LİMİTLİ PAKET
            // =====================================================

            bool canAdd =
                currentCount < maxEmployees.Value;


            return (
                CanAdd: canAdd,
                MaxEmployees: maxEmployees,
                CurrentCount: currentCount
            );
        }
    }
}

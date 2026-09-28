using RezerveApp.Data;
using RezerveApp.Services;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.HostedServices
{
    // Her dakika çalışıp, randevusuna tam 24 saat kalan (ertesi gün)
    // ve henüz hatırlatması gönderilmemiş onaylı randevular için
    // müşteriye SMS/WhatsApp hatırlatması gönderir.
    public class AppointmentReminderHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderHostedService> _logger;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

        public AppointmentReminderHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Randevu hatırlatma servisinde hata oluştu.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Uygulama kapanırken beklenen davranış.
                }
            }
        }

        private async Task SendDueRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);

            // Polling aralığı 1 dakika olduğu için tam 24 saat sonrasını hedefler.
            // Yani randevusuna 23 saat 59 dakika ile 24 saat kalanlar.
            var windowStart = now.TimeOfDay;
            var windowEnd = now.AddMinutes(2).TimeOfDay;

            var dueAppointments = await context.Appointments
                .Where(a => a.AppointmentDate.Date == tomorrow &&
                            a.Status == "Approved" &&
                            !a.ReminderSent &&
                            a.StartTime >= windowStart &&
                            a.StartTime <= windowEnd)
                .ToListAsync(stoppingToken);

            foreach (var appointment in dueAppointments)
            {
                await notificationService.NotifyCustomer1DayReminderAsync(appointment);

                appointment.ReminderSent = true;
            }

            if (dueAppointments.Count > 0)
                await context.SaveChangesAsync(stoppingToken);
        }
    }
}

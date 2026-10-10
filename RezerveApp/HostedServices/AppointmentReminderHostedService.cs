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
            // Şimdiki zamana 2 saat ekliyoruz.
            // Yani şu andan tam 2 saat sonra başlayacak randevular.
            var targetTime = now.AddHours(2);
            var targetDate = targetTime.Date;
            var windowStart = targetTime.TimeOfDay;
            var windowEnd = targetTime.AddMinutes(2).TimeOfDay;

            var dueAppointments = await context.Appointments
                .Where(a => a.AppointmentDate.Date == targetDate &&
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

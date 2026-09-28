using System.Globalization;
using RezerveApp.Data;
using RezerveApp.Models;
using Microsoft.EntityFrameworkCore;

using RezerveApp.Services.Sms;

namespace RezerveApp.Services
{
    // Randevu olaylarına bağlı WhatsApp bildirimlerini oluşturur, gönderir
    // ve sonucu (başarılı/başarısız) NotificationLog tablosuna kaydeder.
    // Gönderim başarısız olsa bile bu servis exception fırlatmaz —
    // randevu akışının bildirim yüzünden bozulmaması için.
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppService _whatsApp;
        private readonly ISmsProvider _smsProvider;

        public NotificationService(ApplicationDbContext context, IWhatsAppService whatsApp, ISmsProvider smsProvider)
        {
            _context = context;
            _whatsApp = whatsApp;
            _smsProvider = smsProvider;
        }

        // Türkiye'ye özgü 11 haneli (0XXXXXXXXXX) numarayı WhatsApp'ın
        // beklediği E.164 benzeri formata (90XXXXXXXXXX) çevirir.
        private static string NormalizePhone(string phone)
        {
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("0"))
                return "90" + digits.Substring(1);

            if (digits.Length == 12 && digits.StartsWith("90"))
                return digits;

            if (digits.Length == 10)
                return "90" + digits;

            return digits;
        }

        private async Task SendAndLogAsync(
            NotificationType type,
            int businessId,
            int? appointmentId,
            string recipientPhone,
            string message)
        {
            WhatsAppSendResult result;

            try
            {
                result = await _whatsApp.SendMessageAsync(NormalizePhone(recipientPhone), message);
            }
            catch (Exception ex)
            {
                result = new WhatsAppSendResult { Success = false, ErrorMessage = ex.Message };
            }

            _context.NotificationLogs.Add(new NotificationLog
            {
                Type = type,
                BusinessId = businessId,
                AppointmentId = appointmentId,
                RecipientPhone = recipientPhone,
                Message = message,
                IsSuccess = result.Success,
                ErrorMessage = result.ErrorMessage
            });

            await _context.SaveChangesAsync();
        }

        // İşletmeye (berbere) yeni randevu talebi bildirimi.
        public async Task NotifyBusinessNewAppointmentAsync(Appointment appointment)
        {
            var business = await _context.Businesses.FindAsync(appointment.BusinessId);
            var employee = await _context.Employees.FindAsync(appointment.EmployeeId);
            var service = await _context.Services.FindAsync(appointment.ServiceId);

            if (business == null || string.IsNullOrWhiteSpace(business.Phone))
                return;

            var message =
                $"Yeni randevu talebi:\n" +
                $"Müşteri: {appointment.CustomerName} ({appointment.CustomerPhone})\n" +
                $"Hizmet: {service?.Name}\n" +
                $"Personel: {employee?.Name}\n" +
                $"Tarih: {appointment.AppointmentDate:dd.MM.yyyy} {appointment.StartTime:hh\\:mm}\n" +
                $"Durum: Onay bekliyor.";

            await SendAndLogAsync(NotificationType.NewAppointmentBusiness, business.Id, appointment.Id, business.Phone, message);
        }

        // Müşteriye randevu onay/alındı mesajı.
        public async Task NotifyCustomerConfirmationAsync(Appointment appointment)
        {
            var business = await _context.Businesses.FindAsync(appointment.BusinessId);
            var employee = await _context.Employees.FindAsync(appointment.EmployeeId);
            var service = await _context.Services.FindAsync(appointment.ServiceId);

            if (business == null || string.IsNullOrWhiteSpace(appointment.CustomerPhone))
                return;

            var message =
                $"{business.Name} - Randevunuz alındı.\n" +
                $"Hizmet: {service?.Name}\n" +
                $"Personel: {employee?.Name}\n" +
                $"Tarih: {appointment.AppointmentDate:dd.MM.yyyy} {appointment.StartTime:hh\\:mm}\n" +
                $"Randevunuzu iptal etmeniz gerekirse lütfen işletmeyi arayın.";

            await SendAndLogAsync(NotificationType.AppointmentConfirmationCustomer, business.Id, appointment.Id, appointment.CustomerPhone, message);
        }

        // Randevudan ~30 dakika önce müşteriye hatırlatma.
        public async Task NotifyCustomerReminderAsync(Appointment appointment)
        {
            var business = await _context.Businesses.FindAsync(appointment.BusinessId);

            if (business == null || string.IsNullOrWhiteSpace(appointment.CustomerPhone))
                return;

            var message =
                $"Hatırlatma: {business.Name} adresinde bugün saat " +
                $"{appointment.StartTime:hh\\:mm} randevunuz var. Sizi bekliyoruz!";

            await SendAndLogAsync(NotificationType.AppointmentReminderCustomer, business.Id, appointment.Id, appointment.CustomerPhone, message);
        }
        // Randevudan 1 gün önce (24 saat) müşteriye otomatik SMS hatırlatması.
        public async Task NotifyCustomer1DayReminderAsync(Appointment appointment)
        {
            var business = await _context.Businesses.FindAsync(appointment.BusinessId);
            var service = await _context.Services.FindAsync(appointment.ServiceId);

            if (business == null || string.IsNullOrWhiteSpace(appointment.CustomerPhone))
                return;

            var message =
                $"Sayın {appointment.CustomerName}, yarın saat {appointment.StartTime:hh\\:mm}'de " +
                $"{business.Name} işletmesinde {service?.Name} randevunuz bulunmaktadır. Lütfen geç kalmayınız!";

            bool isSuccess = false;
            string? errorMessage = null;

            try
            {
                isSuccess = await _smsProvider.SendSmsAsync(appointment.CustomerPhone, message);
                if (!isSuccess)
                {
                    errorMessage = "SMS sağlayıcı false döndü.";
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = ex.Message;
            }

            _context.NotificationLogs.Add(new NotificationLog
            {
                Type = NotificationType.AppointmentReminderCustomer,
                BusinessId = business.Id,
                AppointmentId = appointment.Id,
                RecipientPhone = appointment.CustomerPhone,
                Message = message,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            });

            await _context.SaveChangesAsync();
        }
    }
}

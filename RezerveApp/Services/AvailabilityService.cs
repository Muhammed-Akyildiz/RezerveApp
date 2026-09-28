using RezerveApp.Data;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Services
{
    // Bir randevunun belirli bir çalışan/tarih/saat için geçerli olup
    // olmadığını kontrol eden kurallar (çalışma saati, işletme saati,
    // tatil, izin, mola, çakışma). Takvim sürükle-bırak akışında kullanılır.
    public class AvailabilityService
    {
        private readonly ApplicationDbContext _context;

        public AvailabilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Uygunsa null, değilse kullanıcıya gösterilecek hata mesajını döner.
        // excludeAppointmentId: taşınan randevunun kendisiyle çakışma
        // kontrolüne takılmaması için (reschedule senaryosu).
        public async Task<string?> ValidateSlotAsync(
            int businessId,
            int employeeId,
            int durationMinutes,
            DateTime date,
            TimeSpan startTime,
            int? excludeAppointmentId = null)
        {
            if (date.Date < DateTime.Today)
                return "Geçmiş bir tarihe randevu taşınamaz.";

            var isHoliday = await _context.BusinessHolidays
                .AnyAsync(h => h.BusinessId == businessId && h.Date.Date == date.Date);

            if (isHoliday)
                return "İşletme bu tarihte kapalı.";

            var businessHour = await _context.BusinessWorkingHours
                .FirstOrDefaultAsync(x => x.BusinessId == businessId && x.DayOfWeek == date.DayOfWeek);

            if (businessHour != null && !businessHour.IsOpen)
                return "İşletme bu gün kapalı.";

            var workingHour = await _context.WorkingHours
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.DayOfWeek == date.DayOfWeek);

            if (workingHour == null || !workingHour.IsWorking)
                return "Çalışan bu gün çalışmıyor.";

            var endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));

            var dayStart = workingHour.StartTime;
            var dayEnd = workingHour.EndTime;

            if (businessHour != null && businessHour.IsOpen)
            {
                if (businessHour.OpenTime > dayStart) dayStart = businessHour.OpenTime;
                if (businessHour.CloseTime < dayEnd) dayEnd = businessHour.CloseTime;
            }

            if (startTime < dayStart || endTime > dayEnd)
                return "Seçilen saat çalışma saatleri dışında.";

            var onLeave = await _context.EmployeeTimeOffs
                .AnyAsync(t => t.EmployeeId == employeeId &&
                               date.Date >= t.StartDate.Date && date.Date <= t.EndDate.Date);

            if (onLeave)
                return "Çalışan bu tarihte izinli.";

            var onBreak = await _context.EmployeeBreaks
                .AnyAsync(b => b.EmployeeId == employeeId && b.DayOfWeek == date.DayOfWeek &&
                               startTime < b.EndTime && endTime > b.StartTime);

            if (onBreak)
                return "Seçilen saat çalışanın molasına denk geliyor.";

            var conflictQuery = _context.Appointments
                .Where(x => x.EmployeeId == employeeId &&
                            x.AppointmentDate.Date == date.Date &&
                            x.Status != "Cancelled" && x.Status != "Rejected" &&
                            x.StartTime < endTime && x.EndTime > startTime);

            if (excludeAppointmentId.HasValue)
                conflictQuery = conflictQuery.Where(x => x.Id != excludeAppointmentId.Value);

            if (await conflictQuery.AnyAsync())
                return "Bu saat çalışanın başka bir randevusuyla çakışıyor.";

            return null;
        }
    }
}

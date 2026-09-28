namespace RezerveApp.Models
{
    // İşletmenin gün bazlı açılış/kapanış saatleri.
    // Not: Employee bazlı çalışma saatleri için mevcut WorkingHour
    // modeli kullanılmaya devam eder; bu tablo işletmenin genel
    // (dış) çalışma saatlerini temsil eder.
    public class BusinessWorkingHour
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan OpenTime { get; set; }

        public TimeSpan CloseTime { get; set; }

        public bool IsOpen { get; set; } = true;
    }
}

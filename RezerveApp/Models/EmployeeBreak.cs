namespace RezerveApp.Models
{
    // Çalışanın haftalık düzenli molası (öğle arası vb.).
    // Randevu müsaitlik hesabında bu aralık dolu kabul edilir.
    public class EmployeeBreak
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }
    }
}

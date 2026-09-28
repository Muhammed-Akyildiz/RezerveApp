namespace RezerveApp.Models
{
    // Çalışanın izinli olduğu tarih aralığı (tek gün için
    // StartDate == EndDate verilebilir).
    public class EmployeeTimeOff
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? Reason { get; set; }
    }
}

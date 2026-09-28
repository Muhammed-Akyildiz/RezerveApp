namespace RezerveApp.Models
{
    public class WorkingHour
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public Employee? Employee { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsWorking { get; set; } = true;
    }
}
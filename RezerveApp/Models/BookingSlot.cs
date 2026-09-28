namespace RezerveApp.Models
{
    public class BookingSlot
    {
        public TimeSpan Time { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;
    }
}

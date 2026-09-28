namespace RezerveApp.Models
{
    // İşletmenin tamamen kapalı olduğu gün (resmi tatil, özel gün vb.)
    public class BusinessHoliday
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public Business? Business { get; set; }

        public DateTime Date { get; set; }

        public string? Reason { get; set; }
    }
}

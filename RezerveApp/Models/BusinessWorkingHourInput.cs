namespace RezerveApp.Models
{
    // Views/Business/WorkingHours.cshtml formundaki "WorkingHours[i].*" alanlarına
    // karşılık gelen, sadece model-binding için kullanılan yardımcı sınıf.
    public class BusinessWorkingHourInput
    {
        public bool IsOpen { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
    }
}

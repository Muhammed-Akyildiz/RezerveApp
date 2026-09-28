namespace RezerveApp.Models
{
    // SuperAdmin > İşletme Detay sayfasını beslemek için kullanılan
    // salt-okunur görüntüleme modeli.
    public class BusinessDetailsViewModel
    {
        public Business Business { get; set; } = null!;

        public List<Employee> Employees { get; set; } = new();

        public List<Service> Services { get; set; } = new();

        public List<Appointment> Appointments { get; set; } = new();

        public BusinessSubscription? ActiveSubscription { get; set; }

        public List<SubscriptionPlan> AvailablePlans { get; set; } = new();
        public string? GalleryImage1 { get; set; }
        public string? GalleryImage2 { get; set; }
        public string? GalleryImage3 { get; set; }
    }
}

namespace RezerveApp.Models
{
    // Bir çalışanın verebildiği hizmetleri temsil eden ilişki tablosu.
    // Randevu akışında (Faz 4) bir hizmet seçildiğinde sadece bu
    // hizmeti verebilen çalışanlar listelenecek.
    public class EmployeeService
    {
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public int ServiceId { get; set; }
        public Service? Service { get; set; }
    }
}

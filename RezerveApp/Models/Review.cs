using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RezerveApp.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        public int BusinessId { get; set; }
        
        [ForeignKey("BusinessId")]
        public Business Business { get; set; } = null!;

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; } = null!;

        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public Appointment Appointment { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; } // 1-5 yıldız

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

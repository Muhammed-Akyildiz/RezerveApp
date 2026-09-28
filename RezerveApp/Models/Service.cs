using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100000)]
        public decimal Price { get; set; }

        [Range(5, 480)]
        public int DurationMinutes { get; set; }

        public int BusinessId { get; set; }

        public Business? Business { get; set; }
    }
}
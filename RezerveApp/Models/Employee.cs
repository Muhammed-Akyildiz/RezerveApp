using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public int BusinessId { get; set; }

        public Business? Business { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
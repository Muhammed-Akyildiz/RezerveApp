using Microsoft.AspNetCore.Identity;

namespace RezerveApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public int? BusinessId { get; set; }
        public Business? Business { get; set; }

        public int? EmployeeId { get; set; }
    }
}
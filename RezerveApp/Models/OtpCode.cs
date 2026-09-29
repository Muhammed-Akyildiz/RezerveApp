using System;
using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    public class OtpCode
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; }
        
        [Required]
        [MaxLength(6)]
        public string Code { get; set; }
        
        public DateTime ExpirationTime { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Purpose { get; set; }
        
        public bool IsUsed { get; set; }
    }
}

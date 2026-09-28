using System.ComponentModel.DataAnnotations;

namespace RezerveApp.Models
{
    // Sistemdeki dinamik anahtar-değer ayarlarını tutar
    public class SystemSetting
    {
        [Key]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        public string? Value { get; set; }
        
        // Açıklama (UI'da göstermek için)
        [StringLength(255)]
        public string? Description { get; set; }
    }
}

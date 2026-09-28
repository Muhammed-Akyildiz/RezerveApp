import io
import re

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Controllers\BusinessController.cs'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_sig = 'public async Task<IActionResult> SaveProfile(int id, string name, string phone, string instagram, string city, string district, string address, string neighborhood, IFormFile? logo, string? latitude, string? longitude, int slotInterval = 30)'
new_sig = 'public async Task<IActionResult> SaveProfile(int id, string name, string phone, string instagram, string city, string district, string address, string neighborhood, IFormFile? logo, IFormFile? gallery1, IFormFile? gallery2, IFormFile? gallery3, string? latitude, string? longitude, int slotInterval = 30)'

content = content.replace(old_sig, new_sig)

old_save = '''                if (logo != null && logo.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(logo.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logos", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream);
                    }
                    biz.LogoUrl = "/images/logos/" + fileName;
                }'''

new_save = old_save + '''

                if (gallery1 != null && gallery1.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(gallery1.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery", fileName);
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery"));
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await gallery1.CopyToAsync(stream); }
                    biz.GalleryImage1 = "/images/gallery/" + fileName;
                }
                if (gallery2 != null && gallery2.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(gallery2.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery", fileName);
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery"));
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await gallery2.CopyToAsync(stream); }
                    biz.GalleryImage2 = "/images/gallery/" + fileName;
                }
                if (gallery3 != null && gallery3.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(gallery3.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery", fileName);
                    Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/gallery"));
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await gallery3.CopyToAsync(stream); }
                    biz.GalleryImage3 = "/images/gallery/" + fileName;
                }'''

content = content.replace(old_save, new_save)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated BusinessController.')

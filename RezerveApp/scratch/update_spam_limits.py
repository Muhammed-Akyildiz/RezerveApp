import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Controllers\BookingController.cs'
with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_spam = '''                var pendingAppointmentsCount = await _context.Appointments
                    .CountAsync(a => a.BusinessId == biz.Id &&
                                     a.CustomerPhone == currentPhone &&
                                     a.Status == "Pending");

                if (pendingAppointmentsCount + selectedServiceIds.Length > 4)
                {
                    return BadRequest("Çok fazla onay bekleyen randevunuz var. Lütfen önce işletmenin mevcut taleplerinizi onaylamasını bekleyin.");
                }'''

new_spam = '''                var pendingAppointmentsCount = await _context.Appointments
                    .CountAsync(a => a.BusinessId == biz.Id &&
                                     a.CustomerPhone == currentPhone &&
                                     a.Status == "Pending");

                if (selectedServiceIds.Length > 4)
                {
                    TempData["ErrorMessage"] = "Tek seferde en fazla 4 hizmet için randevu alabilirsiniz.";
                    return RedirectToAction("Index", new { slug = slug });
                }

                if (pendingAppointmentsCount + selectedServiceIds.Length > 4)
                {
                    TempData["ErrorMessage"] = "Çok fazla onay bekleyen randevunuz var. Yeni randevu almadan önce işletmenin mevcut taleplerinizi onaylamasını bekleyin.";
                    return RedirectToAction("Index", new { slug = slug });
                }'''

content = content.replace(old_spam, new_spam)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Updated BookingController spam checks.')

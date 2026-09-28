import io

path = r'c:\Users\Asus\OneDrive\Desktop\RezerveApp\RezerveApp\Controllers\ReviewController.cs'

with io.open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old_get_logic = '''            // Daha önce değerlendirme yapılmış mı?
            var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (existingReview != null)
            {
                ViewBag.Message = "Bu randevuyu daha önce değerlendirdiniz. Teşekkür ederiz!";
                return View("Success");
            }'''

new_get_logic = '''            // Süre kontrolü (3 gün)
            if ((DateTime.UtcNow.Date - appointment.AppointmentDate.Date).TotalDays > 3)
            {
                ViewBag.Message = "Bu randevu için değerlendirme süresi (3 gün) dolmuştur.";
                return View("Success");
            }

            // Daha önce değerlendirme yapılmış mı?
            var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (existingReview != null)
            {
                ViewBag.Message = "Bu randevuyu daha önce değerlendirdiniz. Teşekkür ederiz!";
                return View("Success");
            }'''

content = content.replace(old_get_logic, new_get_logic)


old_post_logic = '''            var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (existingReview != null)
            {
                ViewBag.Message = "Bu randevuyu zaten değerlendirdiniz.";
                return View("Success");
            }'''

new_post_logic = '''            // Süre kontrolü (3 gün)
            if ((DateTime.UtcNow.Date - appointment.AppointmentDate.Date).TotalDays > 3)
            {
                ViewBag.Message = "Bu randevu için değerlendirme süresi (3 gün) dolmuştur.";
                return View("Success");
            }

            var existingReview = await _context.Reviews.FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);
            if (existingReview != null)
            {
                ViewBag.Message = "Bu randevuyu zaten değerlendirdiniz.";
                return View("Success");
            }'''

content = content.replace(old_post_logic, new_post_logic)

with io.open(path, 'w', encoding='utf-8') as f:
    f.write(content)

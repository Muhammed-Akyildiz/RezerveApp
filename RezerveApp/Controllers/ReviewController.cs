using RezerveApp.Data;
using RezerveApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Review/Leave/{appointmentId}")]
        public async Task<IActionResult> Leave(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Business)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound("Randevu bulunamadı.");
            }

            // Randevu tamamlanmış mı kontrol edilebilir (İsteğe bağlı)
            if (appointment.Status != "Completed")
            {
                // return BadRequest("Sadece tamamlanmış randevuları değerlendirebilirsiniz.");
            }

            // Süre kontrolü (3 gün)
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
            }

            return View(appointment);
        }

        [HttpPost("Review/Leave/{appointmentId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int appointmentId, int rating, string? comment)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Business)
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null || !appointment.CustomerId.HasValue)
                return NotFound("Geçersiz randevu veya müşteri.");

            // Süre kontrolü (3 gün)
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
            }

            if (rating < 1 || rating > 5)
                rating = 5; // Default

            var review = new Review
            {
                AppointmentId = appointment.Id,
                BusinessId = appointment.BusinessId,
                CustomerId = appointment.CustomerId.Value,
                EmployeeId = appointment.EmployeeId,
                Rating = rating,
                Comment = comment?.Trim()
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            ViewBag.Message = "Değerlendirmeniz başarıyla kaydedildi. Bizi tercih ettiğiniz için teşekkür ederiz!";
            return View("Success");
        }
    }
}

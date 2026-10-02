using System.Data;
using RezerveApp.Data;
using RezerveApp.Models;
using RezerveApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public BookingController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Bir işletmenin belirli bir günde müşteri tarafından
        // görülebilir/rezerve edilebilir olup olmadığını kontrol eder.
        private static bool IsBookable(Business business)
        {
            return business.IsActive && business.Status == BusinessStatus.Approved;
        }

        // =========================================================
        // MÜŞTERİ RANDEVU SAYFASI
        // /Booking/{slug}
        // =========================================================
        [HttpGet("Booking/{slug}")]
        public async Task<IActionResult> Index(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == slug);

            if (business == null || !IsBookable(business))
                return NotFound();

            var services = await _context.Services
                .AsNoTracking()
                .Where(x => x.BusinessId == business.Id)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var employees = await _context.Employees
                .AsNoTracking()
                .Where(x => x.BusinessId == business.Id && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(x => x.BusinessId == business.Id)
                .ToListAsync();
            double avgRating = reviews.Any() ? reviews.Average(x => x.Rating) : 0;
            ViewBag.ReviewCount = reviews.Count;
            ViewBag.AvgRating = avgRating;

            // Hangi çalışan hangi hizmeti veriyor? (Faz 3'te tanımlanan
            // kısıtlama varsa) — JS tarafında berber adımını hizmete göre
            // filtrelemek için kullanılabilir.
            var employeeServiceMap = await _context.EmployeeServices
                .Where(es => es.Employee!.BusinessId == business.Id)
                .Select(es => new { es.EmployeeId, es.ServiceId })
                .ToListAsync();

            var businessClosedDays = await _context.BusinessWorkingHours
                .Where(x => x.BusinessId == business.Id && !x.IsOpen)
                .Select(x => (int)x.DayOfWeek)
                .ToListAsync();

            var businessHolidays = await _context.BusinessHolidays
                .Where(x => x.BusinessId == business.Id)
                .Select(x => x.Date.ToString("yyyy-MM-dd"))
                .ToListAsync();

            var employeeClosedDays = await _context.WorkingHours
                .Where(x => x.Employee!.BusinessId == business.Id && !x.IsWorking)
                .Select(x => new { x.EmployeeId, Day = (int)x.DayOfWeek })
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.Services = services;
            ViewBag.Employees = employees;
            ViewBag.EmployeeServiceMap = employeeServiceMap;
            ViewBag.BusinessClosedDays = businessClosedDays;
            ViewBag.BusinessHolidays = businessHolidays;
            ViewBag.EmployeeClosedDays = employeeClosedDays;

            return View();
        }

        // =========================================================
        // MÜSAİT SAATLER
        // /Booking/{slug}/Availability
        //
        // employeeId = 0 ise "Fark Etmez" anlamına gelir.
        // Bu durumda bütün aktif berberler kontrol edilir ve
        // her saat için uygun olan ilk berber atanır.
        // =========================================================
        [HttpGet("Booking/{slug}/Availability")]
        public async Task<IActionResult> Availability(
            string slug,
            int employeeId,
            string serviceIds,
            DateTime date)
        {
            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == slug);

            if (business == null || !IsBookable(business))
                return NotFound();

            var ids = serviceIds.Split(',').Select(int.Parse).ToList();
            var services = await _context.Services
                .AsNoTracking()
                .Where(x => ids.Contains(x.Id) && x.BusinessId == business.Id)
                .ToListAsync();

            if (services.Count == 0)
                return NotFound();

            if (date.Date < DateTime.Today)
                return BadRequest("Geçmiş bir tarih seçilemez.");

            if (date.Date > DateTime.Today.AddMonths(2))
                return BadRequest("En fazla 2 ay sonrasına randevu alabilirsiniz.");

            // İşletme o gün tatil mi? (resmi tatil / özel kapalı gün)
            var isHoliday = await _context.BusinessHolidays
                .AsNoTracking()
                .AnyAsync(h => h.BusinessId == business.Id && h.Date.Date == date.Date);

            if (isHoliday)
            {
                ViewBag.Business = business;
                ViewBag.Services = services;
                ViewBag.Date = date;
                ViewBag.AvailableSlots = new List<BookingSlot>();
                ViewBag.ClosedMessage = "İşletme bu tarihte kapalı.";
                return View();
            }

            // İşletmenin o gün için tanımlı genel çalışma saati var mı?
            var businessHour = await _context.BusinessWorkingHours
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BusinessId == business.Id && x.DayOfWeek == date.DayOfWeek);

            if (businessHour != null && !businessHour.IsOpen)
            {
                ViewBag.Business = business;
                ViewBag.Services = services;
                ViewBag.Date = date;
                ViewBag.AvailableSlots = new List<BookingSlot>();
                ViewBag.ClosedMessage = "İşletme bu gün kapalı.";
                return View();
            }

            List<Employee> employees;

            if (employeeId > 0)
            {
                var employee = await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == business.Id && x.IsActive);

                if (employee == null)
                    return NotFound();

                var explicitWorkingHour = await _context.WorkingHours
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.DayOfWeek == date.DayOfWeek);

                if (explicitWorkingHour != null && !explicitWorkingHour.IsWorking)
                {
                    ViewBag.Business = business;
                    ViewBag.Services = services;
                    ViewBag.Date = date;
                    ViewBag.AvailableSlots = new List<BookingSlot>();
                    ViewBag.ClosedMessage = "Seçilen çalışan bugün hizmet vermiyor.";
                    return View();
                }

                var onLeave = await _context.EmployeeTimeOffs
                    .AsNoTracking()
                    .AnyAsync(t => t.EmployeeId == employee.Id &&
                                   date.Date >= t.StartDate.Date && date.Date <= t.EndDate.Date);

                if (onLeave)
                {
                    ViewBag.Business = business;
                    ViewBag.Services = services;
                    ViewBag.Date = date;
                    ViewBag.AvailableSlots = new List<BookingSlot>();
                    ViewBag.ClosedMessage = "Seçilen çalışan bu tarihte izinli.";
                    return View();
                }

                employees = new List<Employee> { employee };
            }
            else
            {
                employees = await _context.Employees
                    .AsNoTracking()
                    .Where(x => x.BusinessId == business.Id && x.IsActive)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            }

            // Bu hizmeti verebilen çalışanlarla sınırla (kısıtlama tanımlıysa)
            foreach(var s in services)
            {
                var capableEmployeeIds = await GetCapableEmployeeIdsAsync(employees.Select(e => e.Id).ToList(), s.Id);
                employees = employees.Where(e => capableEmployeeIds.Contains(e.Id)).ToList();
            }

            var slots = new List<BookingSlot>();
            var slotInterval = business.SlotInterval > 0 ? business.SlotInterval : 30;

            foreach (var employee in employees)
            {
                var employeeSlots = await ComputeEmployeeSlotsAsync(employee, services.Sum(s => s.DurationMinutes), date, businessHour, slotInterval);

                foreach (var slot in employeeSlots)
                {
                    var existingSlot = slots.FirstOrDefault(x => x.Time == slot.Time);
                    if (existingSlot == null)
                    {
                        slots.Add(slot);
                    }
                    else
                    {
                        // Eğer önceden eklenen saat doluysa ama bu berberin saati uygunsa, güncelleyelim.
                        if (!existingSlot.IsAvailable && slot.IsAvailable)
                        {
                            slots.Remove(existingSlot);
                            slots.Add(slot);
                        }
                    }
                }
            }

            var orderedSlots = slots.OrderBy(x => x.Time).ToList();

            ViewBag.Business = business;
            ViewBag.Services = services;
            ViewBag.Date = date;
            ViewBag.AvailableSlots = orderedSlots;

            return View();
        }

        // Bir çalışan için verilen tarihte, tüm kısıtlar (çalışma saati,
        // işletme saati, mola, izin, çakışma) uygulanmış boş saat listesi.
        private async Task<List<BookingSlot>> ComputeEmployeeSlotsAsync(
            Employee employee, int durationMinutes, DateTime date, BusinessWorkingHour? businessHour, int slotInterval)
        {
            var result = new List<BookingSlot>();

            var workingHour = await _context.WorkingHours
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.DayOfWeek == date.DayOfWeek);

            TimeSpan dayStart;
            TimeSpan dayEnd;

            if (workingHour != null)
            {
                if (!workingHour.IsWorking)
                    return result;
                
                dayStart = workingHour.StartTime;
                dayEnd = workingHour.EndTime;
            }
            else if (businessHour != null)
            {
                if (!businessHour.IsOpen)
                    return result;

                dayStart = businessHour.OpenTime;
                dayEnd = businessHour.CloseTime;
            }
            else
            {
                // Varsayılan çalışma saatleri 09:00 - 19:00
                dayStart = new TimeSpan(9, 0, 0);
                dayEnd = new TimeSpan(19, 0, 0);
            }

            // Çalışanın izinli olduğu bir güne denk geliyor mu?
            var onLeave = await _context.EmployeeTimeOffs
                .AsNoTracking()
                .AnyAsync(t => t.EmployeeId == employee.Id &&
                               date.Date >= t.StartDate.Date && date.Date <= t.EndDate.Date);

            if (onLeave)
                return result;

            // İşletme geneli saatleri tanımlıysa, çalışanın saatleriyle kesiştir.
            if (businessHour != null && businessHour.IsOpen && workingHour != null)
            {
                if (businessHour.OpenTime > dayStart) dayStart = businessHour.OpenTime;
                if (businessHour.CloseTime < dayEnd) dayEnd = businessHour.CloseTime;
            }

            var breaks = await _context.EmployeeBreaks
                .AsNoTracking()
                .Where(b => b.EmployeeId == employee.Id && b.DayOfWeek == date.DayOfWeek)
                .ToListAsync();

            var existingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(x => x.EmployeeId == employee.Id &&
                            x.AppointmentDate.Date == date.Date &&
                            x.Status != "Cancelled" && x.Status != "Rejected")
                .ToListAsync();

            var duration = TimeSpan.FromMinutes(durationMinutes);
            var current = dayStart;

            while (current.Add(duration) <= dayEnd)
            {
                if (date.Date == DateTime.Today && current <= DateTime.Now.TimeOfDay)
                {
                    current = current.Add(TimeSpan.FromMinutes(slotInterval));
                    continue;
                }

                var slotEnd = current.Add(duration);

                var onBreak = breaks.Any(b => current < b.EndTime && slotEnd > b.StartTime);
                var conflict = existingAppointments.Any(a => current < a.EndTime && slotEnd > a.StartTime);

                result.Add(new BookingSlot
                {
                    Time = current,
                    EmployeeId = employee.Id,
                    EmployeeName = employee.Name,
                    IsAvailable = (!onBreak && !conflict)
                });

                current = current.Add(TimeSpan.FromMinutes(slotInterval));
            }

            return result;
        }

        // Belirli çalışanlardan hangileri, verilen hizmeti verebilir?
        // Bir çalışan için hiç EmployeeService kaydı yoksa kısıtlama
        // uygulanmaz (o çalışan tüm hizmetler için müsait kabul edilir).
        private async Task<HashSet<int>> GetCapableEmployeeIdsAsync(List<int> employeeIds, int serviceId)
        {
            if (employeeIds.Count == 0)
                return new HashSet<int>();

            var restrictedEmployeeIds = await _context.EmployeeServices
                .Where(es => employeeIds.Contains(es.EmployeeId))
                .Select(es => es.EmployeeId)
                .Distinct()
                .ToListAsync();

            var capableForService = await _context.EmployeeServices
                .Where(es => employeeIds.Contains(es.EmployeeId) && es.ServiceId == serviceId)
                .Select(es => es.EmployeeId)
                .ToListAsync();

            var result = new HashSet<int>();

            foreach (var id in employeeIds)
            {
                var hasAnyRestriction = restrictedEmployeeIds.Contains(id);

                if (!hasAnyRestriction || capableForService.Contains(id))
                    result.Add(id);
            }

            return result;
        }

        // =========================================================
        // RANDEVU ONAY KODU GÖNDER
        // /Booking/{slug}/SendCustomerOtp
        // =========================================================
        [HttpPost("Booking/{slug}/SendCustomerOtp")]
        public async Task<IActionResult> SendCustomerOtp(string slug, [FromServices] IOtpService otpService, [FromForm] string customerEmail)
        {
            if (string.IsNullOrWhiteSpace(customerEmail))
                return BadRequest(new { success = false, message = "E-posta adresi gereklidir." });

            var success = await otpService.GenerateAndSendOtpAsync(customerEmail, "BookingConfirmation");

            if (success)
                return Ok(new { success = true });

            return BadRequest(new { success = false, message = "Kod gönderilemedi. Lütfen tekrar deneyin." });
        }

        // =========================================================
        // RANDEVU OLUŞTUR
        // /Booking/{slug}/Create
        // =========================================================
        [HttpPost("Booking/{slug}/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [FromServices] IOtpService otpService,
            string slug,
            string customerName,
            string customerPhone,
            string customerEmail,
            string otpCode,
            int employeeId,
            string serviceIds,
            DateTime date,
            TimeSpan time)
        {
            // -------------------------
            // Temel kontroller
            // -------------------------
            if (string.IsNullOrWhiteSpace(customerName))
                return BadRequest("Ad Soyad zorunludur.");

            if (string.IsNullOrWhiteSpace(customerPhone))
                return BadRequest("Telefon zorunludur.");
                
            if (string.IsNullOrWhiteSpace(customerEmail))
                return BadRequest("E-posta zorunludur.");

            if (string.IsNullOrWhiteSpace(otpCode))
            {
                TempData["ErrorMessage"] = "Randevuyu onaylamak için doğrulama kodu gereklidir.";
                return RedirectToAction("Index", new { slug = slug });
            }

            var isValidOtp = await otpService.VerifyOtpAsync(customerEmail, otpCode, "BookingConfirmation");
            if (!isValidOtp)
            {
                TempData["ErrorMessage"] = "Geçersiz veya süresi dolmuş doğrulama kodu.";
                return RedirectToAction("Index", new { slug = slug });
            }

            customerName = customerName.Trim();
            customerPhone = new string(customerPhone.Where(char.IsDigit).ToArray());

            if (customerPhone.Length != 11)
            {
                TempData["ErrorMessage"] = "Telefon numarası 11 haneli olmalıdır.";
                return RedirectToAction("Index", new { slug = slug });
            }

            if (date.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Geçmiş bir tarihe randevu alınamaz.";
                return RedirectToAction("Index", new { slug = slug });
            }

            if (date.Date > DateTime.Today.AddMonths(2))
            {
                TempData["ErrorMessage"] = "En fazla 2 ay sonrasına randevu alabilirsiniz.";
                return RedirectToAction("Index", new { slug = slug });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(x => x.Slug == slug);

            if (business == null || !IsBookable(business))
                return NotFound();

            var ids = serviceIds.Split(',').Select(int.Parse).ToList();
            var services = await _context.Services
                .Where(x => ids.Contains(x.Id) && x.BusinessId == business.Id)
                .ToListAsync();

            if (services.Count == 0)
                return NotFound();

            var pendingCount = await _context.Appointments
                .CountAsync(a => a.BusinessId == business.Id && a.CustomerPhone == customerPhone && a.Status == "Pending");
            
            if (pendingCount + services.Count > 4)
            {
                TempData["ErrorMessage"] = "Aynı anda en fazla 4 hizmet/randevu alabilirsiniz. Lütfen önce işletmenin mevcut taleplerinizi onaylamasını bekleyin.";
                return RedirectToAction("Index", new { slug = slug });
            }

            // İşletme tatil mi?
            var isHoliday = await _context.BusinessHolidays
                .AnyAsync(h => h.BusinessId == business.Id && h.Date.Date == date.Date);

            if (isHoliday)
            {
                TempData["ErrorMessage"] = "İşletme seçilen tarihte kapalı.";
                return RedirectToAction("Index", new { slug = slug });
            }

            // İşletmenin genel çalışma saati o gün kapalıysa reddet
            var businessHour = await _context.BusinessWorkingHours
                .FirstOrDefaultAsync(x => x.BusinessId == business.Id && x.DayOfWeek == date.DayOfWeek);

            if (businessHour != null && !businessHour.IsOpen)
            {
                TempData["ErrorMessage"] = "İşletme bu gün kapalı.";
                return RedirectToAction("Index", new { slug = slug });
            }

            // =====================================================
            // Eşzamanlılık koruması:
            // Aynı saat/berber için iki isteğin aynı anda onaylanmasını
            // (double-booking) engellemek için işlem Serializable
            // izolasyon seviyesinde yürütülür; çakışma kontrolü ve
            // ekleme aynı transaction içinde, aralık kilidiyle yapılır.
            // =====================================================
            using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                Employee? employee = null;
                TimeSpan totalEndTime = time.Add(TimeSpan.FromMinutes(services.Sum(s => s.DurationMinutes)));

                if (employeeId > 0)
                {
                    employee = await _context.Employees
                        .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == business.Id && x.IsActive);

                    if (employee == null)
                        return NotFound("Berber bulunamadı.");

                    foreach(var s in services)
                    {
                        var capable = await GetCapableEmployeeIdsAsync(new List<int> { employee.Id }, s.Id);
                        if (!capable.Contains(employee.Id))
                        {
                            TempData["ErrorMessage"] = $"Seçilen berber {s.Name} hizmetini vermiyor.";
                            return RedirectToAction("Index", new { slug = slug });
                        }
                    }

                    // Toplam süre kadar müsaitliği validate et
                    var dummyService = new Service { DurationMinutes = services.Sum(s => s.DurationMinutes) };
                    var validation = await ValidateSlotAsync(employee, dummyService, date, time, businessHour);
                    if (validation != null)
                    {
                        TempData["ErrorMessage"] = validation;
                        return RedirectToAction("Index", new { slug = slug });
                    }
                }
                else
                {
                    var possibleEmployees = await _context.Employees
                        .Where(x => x.BusinessId == business.Id && x.IsActive)
                        .OrderBy(x => x.Name)
                        .ToListAsync();

                    var candidateIds = possibleEmployees.Select(e => e.Id).ToList();
                    foreach(var s in services)
                    {
                        var capableIds = await GetCapableEmployeeIdsAsync(candidateIds, s.Id);
                        candidateIds = candidateIds.Intersect(capableIds).ToList();
                    }

                    var dummyService = new Service { DurationMinutes = services.Sum(s => s.DurationMinutes) };
                    foreach (var candidate in possibleEmployees.Where(e => candidateIds.Contains(e.Id)))
                    {
                        var validation = await ValidateSlotAsync(candidate, dummyService, date, time, businessHour);
                        if (validation == null)
                        {
                            employee = candidate;
                            break;
                        }
                    }

                    if (employee == null)
                    {
                        TempData["ErrorMessage"] = "Seçtiğiniz saat artık uygun değil. Lütfen başka bir saat seçin.";
                        return RedirectToAction("Index", new { slug = slug });
                    }
                }

                // Müşteriyi telefon numarasıyla eşleştir / yoksa oluştur
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.BusinessId == business.Id && c.Phone == customerPhone);

                if (customer != null && customer.IsBlacklisted)
                {
                    TempData["ErrorMessage"] = "Sistemimizde daha önce oluşturduğunuz randevulara katılmadığınız tespit edilmiştir. Lütfen işletme ile iletişime geçiniz.";
                    return RedirectToAction(nameof(Index), new { slug = slug });
                }

                if (customer == null)
                {
                    customer = new Customer
                    {
                        BusinessId = business.Id,
                        Name = customerName,
                        Phone = customerPhone,
                        Email = customerEmail
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Eğer email eksikse veya değiştiyse güncelle (tercihe bağlı)
                    if (string.IsNullOrWhiteSpace(customer.Email) || customer.Email != customerEmail)
                    {
                        customer.Email = customerEmail;
                        await _context.SaveChangesAsync();
                    }
                }

                var currentSlotTime = time;
                var groupId = Guid.NewGuid().ToString();
                Appointment? firstApp = null;
                foreach(var s in services)
                {
                    var appointment = new Appointment
                    {
                        CustomerName = customerName,
                        CustomerPhone = customerPhone,
                        CustomerEmail = customerEmail,
                        CustomerId = customer.Id,
                        GroupId = groupId,
                        BusinessId = business.Id,
                        EmployeeId = employee.Id,
                        ServiceId = s.Id,
                        AppointmentDate = date.Date,
                        StartTime = currentSlotTime,
                        EndTime = currentSlotTime.Add(TimeSpan.FromMinutes(s.DurationMinutes)),
                        Status = "Pending"
                    };
                    _context.Appointments.Add(appointment);
                    currentSlotTime = appointment.EndTime;
                    if(firstApp == null) firstApp = appointment;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // İlişkili varlıkları include ederek load et,
                // böylece şablon motoru Name'lere ulaşabilir
                if(firstApp != null) {
                    await _context.Entry(firstApp).Reference(a => a.Business).LoadAsync();
                    await _context.Entry(firstApp).Reference(a => a.Service).LoadAsync();
                    await _context.Entry(firstApp).Reference(a => a.Employee).LoadAsync();
                }

                // WhatsApp bildirimleri: randevu akışını asla bloklamamalı/bozmamalı,
                // bu yüzden hata olsa bile yutulur (NotificationLog'a hatasıyla kaydedilir).
                try
                {
                    if (firstApp != null)
                    {
                        await _notificationService.NotifyBusinessNewAppointmentAsync(firstApp);
                        await _notificationService.NotifyCustomerConfirmationAsync(firstApp);
                    }
                }
                catch
                {
                    // Bildirim hatası randevu oluşturmayı etkilemez.
                }

                // -------------------------
                // Başarı ekranı bilgileri
                // -------------------------
                ViewBag.Business = business;
                ViewBag.BusinessSlug = business.Slug;
                ViewBag.ServiceName = string.Join(", ", services.Select(s => s.Name));
                ViewBag.EmployeeName = employee.Name;
                ViewBag.Date = date.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                ViewBag.Time = time.ToString("hh\\:mm");
                ViewBag.CustomerName = customerName;
                ViewBag.CustomerPhone = customerPhone;
                ViewBag.TotalPrice = services.Sum(s => s.Price).ToString("0.##");
                ViewBag.ConfirmationCode = $"BS-{firstApp?.Id:D6}";

                return View("Success", firstApp);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Tek bir çalışan/saat kombinasyonu için tüm kuralları kontrol
        // eder; uygunsa null, değilse hata mesajı döner.
        private async Task<string?> ValidateSlotAsync(
            Employee employee, Service service, DateTime date, TimeSpan time, BusinessWorkingHour? businessHour)
        {
            var workingHour = await _context.WorkingHours
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.DayOfWeek == date.DayOfWeek);

            TimeSpan dayStart;
            TimeSpan dayEnd;

            if (workingHour != null)
            {
                if (!workingHour.IsWorking)
                    return "Seçilen berber bu gün çalışmıyor.";
                
                dayStart = workingHour.StartTime;
                dayEnd = workingHour.EndTime;
            }
            else if (businessHour != null)
            {
                if (!businessHour.IsOpen)
                    return "İşletme bu gün kapalı.";

                dayStart = businessHour.OpenTime;
                dayEnd = businessHour.CloseTime;
            }
            else
            {
                dayStart = new TimeSpan(9, 0, 0);
                dayEnd = new TimeSpan(19, 0, 0);
            }

            var endTime = time.Add(TimeSpan.FromMinutes(service.DurationMinutes));

            if (businessHour != null && businessHour.IsOpen && workingHour != null)
            {
                if (businessHour.OpenTime > dayStart) dayStart = businessHour.OpenTime;
                if (businessHour.CloseTime < dayEnd) dayEnd = businessHour.CloseTime;
            }

            if (time < dayStart || endTime > dayEnd)
                return "Seçilen saat çalışma saatleri dışında.";

            var onLeave = await _context.EmployeeTimeOffs
                .AnyAsync(t => t.EmployeeId == employee.Id &&
                               date.Date >= t.StartDate.Date && date.Date <= t.EndDate.Date);

            if (onLeave)
                return "Seçilen berber bu tarihte izinli.";

            var onBreak = await _context.EmployeeBreaks
                .AnyAsync(b => b.EmployeeId == employee.Id && b.DayOfWeek == date.DayOfWeek &&
                               time < b.EndTime && endTime > b.StartTime);

            if (onBreak)
                return "Seçilen saat berberin molasına denk geliyor.";

            var conflict = await _context.Appointments
                .AnyAsync(x => x.EmployeeId == employee.Id &&
                               x.AppointmentDate.Date == date.Date &&
                               x.Status != "Cancelled" && x.Status != "Rejected" &&
                               x.StartTime < endTime && x.EndTime > time);

            if (conflict)
                return "Bu saat artık dolu. Lütfen başka bir saat seçin.";

            return null;
        }
        // =========================================================
        // MÜŞTERİ İPTAL AKIŞI
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Cancel(int id, string phone)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Business)
                .Include(a => a.Service)
                .FirstOrDefaultAsync(a => a.Id == id && a.CustomerPhone == phone);

            if (appointment == null)
                return NotFound("Randevu bulunamadı veya telefon numarası eşleşmiyor.");

            var appointmentDateTime = appointment.AppointmentDate.Date.Add(appointment.StartTime);
            var timeRemaining = appointmentDateTime - DateTime.Now;

            ViewBag.CanCancel = true;

            if (appointment.Status == "Cancelled" || appointment.Status == "Rejected")
            {
                ViewBag.CanCancel = false;
                ViewBag.Message = "Bu randevu zaten iptal edilmiş.";
            }
            else if (appointment.Status == "Completed")
            {
                ViewBag.CanCancel = false;
                ViewBag.Message = "Geçmiş randevular iptal edilemez.";
            }
            else if (timeRemaining.TotalHours < 2)
            {
                ViewBag.CanCancel = false;
                ViewBag.Message = "Randevunuza 2 saatten az süre kaldığı için online iptal işlemi yapılamaz. Lütfen işletme ile iletişime geçin.";
            }

            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCancel(int id, string phone)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.CustomerPhone == phone);

            if (appointment == null)
                return NotFound("Randevu bulunamadı.");

            var appointmentDateTime = appointment.AppointmentDate.Date.Add(appointment.StartTime);
            
            if (appointmentDateTime - DateTime.Now < TimeSpan.FromHours(2))
            {
                TempData["Error"] = "Randevuya 2 saatten az süre kaldığı için iptal edilemez.";
                return RedirectToAction("Cancel", new { id, phone });
            }

            if (appointment.Status != "Cancelled" && appointment.Status != "Rejected" && appointment.Status != "Completed")
            {
                appointment.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["Success"] = "Randevunuz başarıyla iptal edildi.";
            }

            return RedirectToAction("Cancel", new { id, phone });
        }
    }
}

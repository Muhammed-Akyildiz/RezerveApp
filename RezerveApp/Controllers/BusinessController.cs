using Microsoft.EntityFrameworkCore;
using RezerveApp.Data;
using RezerveApp.Models;
using RezerveApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace RezerveApp.Controllers
{
    [Authorize(Roles = "BusinessAdmin, Employee")]
    public class BusinessController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionService _subscriptionService;
        private readonly AvailabilityService _availabilityService;
        private readonly NotificationService _notificationService;
        private readonly IConfiguration _configuration;
        private readonly RezerveApp.Services.IImageService _imageService;
        private readonly RezerveApp.Services.Sms.ISmsProvider _smsProvider;
        private readonly IOtpService _otpService;
        private readonly IEmailSender _emailSender;

        public BusinessController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SubscriptionService subscriptionService,
            AvailabilityService availabilityService,
            NotificationService notificationService,
            IConfiguration configuration,
            RezerveApp.Services.IImageService imageService,
            RezerveApp.Services.Sms.ISmsProvider smsProvider,
            IOtpService otpService,
            IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _availabilityService = availabilityService;
            _notificationService = notificationService;
            _configuration = configuration;
            _imageService = imageService;
            _smsProvider = smsProvider;
            _otpService = otpService;
            _emailSender = emailSender;
        }

        // Yardımcı: geçerli kullanıcının işletme id'sini döndürür, yoksa null.
        private async Task<int?> GetCurrentBusinessIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.BusinessId;
        }

        // =====================================================================
        // CREATE BUSINESS
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Create()
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId != null)
                return RedirectToAction(nameof(Index));

            // Kayıt esnasında girilen telefon ve e-postayı otomatik doldur
            ViewBag.RegisteredPhone = TempData["RegisteredPhone"]?.ToString();
            ViewBag.RegisteredEmail = TempData["RegisteredEmail"]?.ToString();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Create(string name, string phone, string email, string city, string district, string address, string? neighborhood, IFormFile? logo, string? latitude, string? longitude)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (user.BusinessId != null)
                return RedirectToAction(nameof(Index));

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError("", "İşletme adı ve telefon zorunludur.");
                return View();
            }

            phone = phone.Trim();
            if (phone.Length != 11)
            {
                ModelState.AddModelError("phone", "Telefon numarası 11 haneli olmalıdır.");
                return View();
            }

            double? parsedLat = null;
            double? parsedLng = null;

            if (!string.IsNullOrWhiteSpace(latitude) && double.TryParse(latitude.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                parsedLat = lat;
                
            if (!string.IsNullOrWhiteSpace(longitude) && double.TryParse(longitude.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                parsedLng = lng;

            // Konum zorunlu
            if (parsedLat == null || parsedLng == null)
            {
                ModelState.AddModelError("latitude", "Harita üzerinden konum seçimi zorunludur.");
                return View();
            }

            var slug = name.ToLower().Replace(" ", "-").Replace("ş", "s").Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o").Replace("ç", "c");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

            if (await _context.Businesses.AnyAsync(b => b.Slug == slug))
            {
                slug += "-" + DateTime.Now.Ticks.ToString().Substring(10);
            }

            if (await _context.Businesses.AnyAsync(b => b.Phone == phone))
            {
                ModelState.AddModelError("phone", "Bu telefon numarası zaten kullanımda.");
                return View();
            }

            if (!string.IsNullOrWhiteSpace(email) && await _context.Businesses.AnyAsync(b => b.Email == email))
            {
                ModelState.AddModelError("email", "Bu e-posta adresi zaten kullanımda.");
                return View();
            }

            string? logoUrl = null;
            if (logo != null && logo.Length > 0)
            {
                if (logo.Length > 10 * 1024 * 1024)
                {
                    ModelState.AddModelError("logo", "Logo dosyası 10MB'dan küçük olmalıdır.");
                    return View();
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("logo", "Sadece JPG, JPEG ve PNG formatları kabul edilmektedir.");
                    return View();
                }

                try
                {
                    logoUrl = await _imageService.UploadImageAsync(logo);
                }
                catch
                {
                    // Cloudinary ayarlanmamışsa hata verme, aşağıda avatar oluştursun
                }
            }
            
            if (string.IsNullOrEmpty(logoUrl))
            {
                // Logo yüklenmemişse işletme adından baş harf avatarı oluştur
                var words = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initials = words.Length >= 2
                    ? $"{words[0][0]}{words[1][0]}".ToUpper()
                    : name.Substring(0, Math.Min(2, name.Length)).ToUpper();

                var colors = new[] { "#C9A96E", "#8B6914", "#6E9FCA", "#5A8A5E", "#C96E9F", "#CA8B6E" };
                var color = colors[Math.Abs(name.GetHashCode()) % colors.Length];

                var svgContent = $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""200"" height=""200"">
  <rect width=""200"" height=""200"" fill=""{color}"" rx=""16""/>
  <text x=""100"" y=""130"" font-family=""Arial,sans-serif"" font-size=""80"" font-weight=""bold"" fill=""white"" text-anchor=""middle"">{initials}</text>
</svg>";
                var base64Svg = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svgContent));
                logoUrl = "data:image/svg+xml;base64," + base64Svg;
            }

            var business = new Business
            {
                Name = name,
                Phone = phone,
                Email = email,
                City = city,
                District = district,
                Address = address ?? "",
                Neighborhood = neighborhood,
                Latitude = parsedLat,
                Longitude = parsedLng,
                Slug = slug,
                LogoUrl = logoUrl,
                Status = BusinessStatus.Approved, // Auto-approve for trial
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            // 14 Günlük Ücretsiz Başlangıç Trial Ata
            var starterPlan = await _context.SubscriptionPlans
                .OrderBy(p => p.MonthlyPrice)
                .FirstOrDefaultAsync();

            if (starterPlan != null)
            {
                var trialSubscription = new BusinessSubscription
                {
                    BusinessId = business.Id,
                    SubscriptionPlanId = starterPlan.Id,
                    BillingPeriod = BillingPeriod.Monthly,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(14),
                    IsActive = true,
                    PaymentStatus = PaymentStatus.Paid // Deneme süresi olduğu için ödendi sayıyoruz
                };
                
                _context.BusinessSubscriptions.Add(trialSubscription);
                await _context.SaveChangesAsync();
            }

            user.BusinessId = business.Id;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // =====================================================================
        // DASHBOARD
        // =====================================================================
        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("BusinessAdmin"))
            {
                return RedirectToAction(nameof(Appointments));
            }
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return RedirectToAction(nameof(Create));

            var business = await _context.Businesses.FindAsync(businessId);

            if (business == null)
                return NotFound();

            if (business.Status != BusinessStatus.Approved || !business.IsActive)
                return RedirectToAction(nameof(Pending));

            var today = DateTime.Today;

            var todaysAppointmentsQuery = _context.Appointments
                .Include(x => x.Employee)
                .Include(x => x.Service)
                .Where(x => x.BusinessId == businessId && x.AppointmentDate.Date == today);

            var appointmentsToday = await todaysAppointmentsQuery.CountAsync();

            var pendingAppointments = await _context.Appointments
                .CountAsync(x => x.BusinessId == businessId && x.Status == "Pending");

            var serviceCount = await _context.Services
                .CountAsync(x => x.BusinessId == businessId);

            var employeeCount = await _context.Employees
                .CountAsync(x => x.BusinessId == businessId && x.IsActive);

            var customerCount = await _context.Customers
                .CountAsync(x => x.BusinessId == businessId);

            // Günlük ciro: bugün onaylanmış/tamamlanmış randevuların hizmet fiyatları toplamı
            var dailyRevenue = await todaysAppointmentsQuery
                .Where(x => x.Status == "Approved" || x.Status == "Completed")
                .Select(x => x.Service!.Price)
                .SumAsync(p => (decimal?)p) ?? 0m;

            // En çok tercih edilen hizmet (son 90 gün, iptal hariç)
            var since = today.AddDays(-90);
            var topService = await _context.Appointments
                .Where(x => x.BusinessId == businessId &&
                            x.AppointmentDate >= since &&
                            x.Status != "Cancelled" && x.Status != "Rejected")
                .GroupBy(x => x.Service!.Name)
                .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync();

            // En verimli personel (son 90 gün, tamamlanan randevu cirosuna göre)
            var topEmployee = await _context.Appointments
                .Where(x => x.BusinessId == businessId &&
                            x.AppointmentDate >= since &&
                            (x.Status == "Approved" || x.Status == "Completed"))
                .GroupBy(x => x.Employee!.Name)
                .Select(g => new { EmployeeName = g.Key, Revenue = g.Sum(a => a.Service!.Price), Count = g.Count() })
                .OrderByDescending(g => g.Revenue)
                .FirstOrDefaultAsync();

            // Puanlama / Değerlendirmeler
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .ToListAsync();
            
            double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            int reviewCount = reviews.Count;

            var upcoming = await _context.Appointments
                .Include(x => x.Employee)
                .Include(x => x.Service)
                .Where(x => x.BusinessId == businessId &&
                            x.Status != "Cancelled" && x.Status != "Rejected" &&
                            (x.AppointmentDate > today ||
                             (x.AppointmentDate == today && x.StartTime > DateTime.Now.TimeOfDay)))
                .OrderBy(x => x.AppointmentDate).ThenBy(x => x.StartTime)
                .Take(6)
                .ToListAsync();

            var todaysRaw = await todaysAppointmentsQuery
                .OrderBy(x => x.StartTime)
                .Select(x => new
                {
                    StartTime = x.StartTime,
                    CustomerName = x.CustomerName,
                    ServiceName = x.Service!.Name,
                    EmployeeName = x.Employee!.Name,
                    Status = x.Status
                })
                .ToListAsync();

            var todaysList = todaysRaw
                .Select(x => new
                {
                    Time = x.StartTime.ToString(@"hh\:mm"),
                    CustomerName = x.CustomerName,
                    ServiceName = x.ServiceName,
                    EmployeeName = x.EmployeeName,
                    Status = x.Status == "Approved"
                        ? "confirmed"
                        : x.Status == "Pending"
                            ? "pending"
                            : "cancelled"
                })
                .ToList();

            ViewBag.BusinessName = business.Name;
            ViewBag.BusinessSlug = business.Slug;
            ViewBag.TodayAppointmentsCount = appointmentsToday;
            ViewBag.PendingAppointmentsCount = pendingAppointments;
            ViewBag.ServicesCount = serviceCount;
            ViewBag.EmployeesCount = employeeCount;
            ViewBag.CustomerCount = customerCount;
            ViewBag.DailyRevenue = dailyRevenue;
            ViewBag.TopServiceName = topService?.ServiceName;
            ViewBag.TopServiceCount = topService?.Count ?? 0;
            ViewBag.TopEmployeeName = topEmployee?.EmployeeName;
            ViewBag.TopEmployeeRevenue = topEmployee?.Revenue ?? 0m;
            ViewBag.UpcomingAppointments = upcoming;
            ViewBag.TodayAppointments = todaysList;



            // Son 30 Günlük Randevu Statü Dağılımı
            var thirtyDaysAgo = today.AddDays(-30);
            var statusQuery = await _context.Appointments
                .Where(x => x.BusinessId == businessId && x.AppointmentDate >= thirtyDaysAgo)
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLabels = new[] { "Onaylandı", "Bekliyor", "Tamamlandı", "İptal/Red" };
            var statusData = new int[4];

            foreach (var item in statusQuery)
            {
                if (item.Status == "Approved") statusData[0] = item.Count;
                else if (item.Status == "Pending") statusData[1] = item.Count;
                else if (item.Status == "Completed") statusData[2] = item.Count;
                else if (item.Status == "Cancelled" || item.Status == "Rejected") statusData[3] += item.Count;
            }

            ViewBag.StatusLabels = System.Text.Json.JsonSerializer.Serialize(statusLabels);
            ViewBag.StatusData = System.Text.Json.JsonSerializer.Serialize(statusData);
            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviewCount;

            return View(business);
        }

        [HttpGet]
        public IActionResult Pending()
        {
            return View();
        }

        // =====================================================================
        // DESTEK
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Support()
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId.Value);

            ViewBag.BusinessName = business?.Name;
            ViewBag.PageTitle = "Destek";
            ViewBag.ActiveNav = "Support";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Support(string subject, string message)
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId.Value);

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Konu ve mesaj zorunludur.";
                ViewBag.BusinessName = business?.Name;
                ViewBag.PageTitle = "Destek";
                ViewBag.ActiveNav = "Support";
                return View();
            }

            // Bildirim olarak süper admine kaydet
            var notification = new RezerveApp.Models.NotificationLog
            {
                BusinessId = businessId.Value,
                Type = RezerveApp.Models.NotificationType.NewAppointmentBusiness,
                RecipientPhone = "SUPPORT",
                Message = $"[DESTEK] {subject}\n\nİşletme: {business?.Name}\n\n{message}",
                CreatedAt = DateTime.UtcNow,
                IsSuccess = true
            };
            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Destek talebiniz alındı. En kısa sürede size dönüş yapılacaktır.";
            ViewBag.BusinessName = business?.Name;
            ViewBag.PageTitle = "Destek";
            ViewBag.ActiveNav = "Support";

            return View();
        }

        // =====================================================================
        // HİZMETLER (CRUD)
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Services()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var services = await _context.Services
                .Where(x => x.BusinessId == businessId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ViewBag.Services = services;

            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(string name, decimal price, int durationMinutes)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(name) || price < 0 || durationMinutes < 5 || durationMinutes > 480)
            {
                TempData["Error"] = "Hizmet bilgileri geçersiz.";
                return RedirectToAction(nameof(Services));
            }

            var (canAdd, maxServices, currentCount) = await _subscriptionService.CanAddServiceAsync(businessId.Value);

            if (!canAdd)
            {
                TempData["Error"] = $"Paketiniz en fazla {maxServices} adet hizmet eklemenize izin vermektedir. Yeni hizmet eklemek için paketinizi yükseltebilirsiniz.";
                return RedirectToAction(nameof(Services));
            }

            var service = new Service
            {
                Name = name.Trim(),
                Price = price,
                DurationMinutes = durationMinutes,
                BusinessId = businessId.Value
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Services));
        }

        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId);

            if (service == null)
                return NotFound();

            return View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, string name, decimal price, int durationMinutes)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId);

            if (service == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name) || price < 0 || durationMinutes < 5 || durationMinutes > 480)
            {
                ModelState.AddModelError("", "Hizmet bilgileri geçersiz.");
                return View(service);
            }

            service.Name = name.Trim();
            service.Price = price;
            service.DurationMinutes = durationMinutes;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == id && s.BusinessId == businessId);

            if (service == null)
                return NotFound();

            var hasAppointments = await _context.Appointments
                .AnyAsync(a => a.ServiceId == id);

            if (hasAppointments)
            {
                TempData["Error"] = "Bu hizmete bağlı randevu kayıtları olduğu için silinemedi.";
                return RedirectToAction(nameof(Services));
            }

            _context.EmployeeServices.RemoveRange(_context.EmployeeServices.Where(es => es.ServiceId == id));
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Services));
        }

        // =====================================================================
        // ÇALIŞANLAR (CRUD)
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Employees()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employees = await _context.Employees
                .Where(x => x.BusinessId == businessId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var (canAdd, maxEmployees, currentCount) = await _subscriptionService.CanAddEmployeeAsync(businessId.Value);

            ViewBag.Employees = employees;
            ViewBag.CanAddEmployee = canAdd;
            ViewBag.MaxEmployees = maxEmployees;
            ViewBag.CurrentEmployeeCount = currentCount;

            return View(employees);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(string name, bool isActive = true, string? email = null)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Çalışan adı zorunludur.";
                return RedirectToAction(nameof(Employees));
            }

            var (canAdd, maxEmployees, _) = await _subscriptionService.CanAddEmployeeAsync(businessId.Value);

            if (!canAdd)
            {
                TempData["Error"] =
                    $"Mevcut paketiniz en fazla {maxEmployees} çalışana izin veriyor. " +
                    "Daha fazla çalışan eklemek için paketinizi yükseltmeniz gerekir.";

                return RedirectToAction(nameof(Employees));
            }

            string? normalizedEmail = null;
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (TempData["OwnerOtpVerified"] as string != "true")
                {
                    TempData["Error"] = "Çalışana giriş izni vermek (şifre oluşturmak) için doğrulama gereklidir.";
                    return RedirectToAction(nameof(Employees));
                }
                TempData.Remove("OwnerOtpVerified");

                normalizedEmail = email.Trim().ToLowerInvariant();
                
                var emailUser = await _userManager.FindByEmailAsync(normalizedEmail);
                if (emailUser != null)
                {
                    TempData["Error"] = "Bu e-posta adresi zaten sistemde kayıtlı. Çalışan oluşturulamadı.";
                    return RedirectToAction(nameof(Employees));
                }
            }

            var employee = new Employee
            {
                Name = name.Trim(),
                BusinessId = businessId.Value,
                IsActive = isActive
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(normalizedEmail))
            {
                var random = new Random();
                var generatedPassword = "R" + random.Next(10000, 99999).ToString();

                var user = new ApplicationUser
                {
                    UserName = normalizedEmail,
                    Email = normalizedEmail,
                    EmailConfirmed = true,
                    BusinessId = businessId.Value,
                    EmployeeId = employee.Id
                };

                var createResult = await _userManager.CreateAsync(user, generatedPassword);
                if (createResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Employee");
                    
                    var htmlMessage = $@"
                        <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #1a1a1a; text-align: center;'>RezerveApp Hesabınız Açıldı</h2>
                            <p style='color: #666; font-size: 16px;'>Merhaba,</p>
                            <p style='color: #666; font-size: 16px;'>İşletmeniz tarafından sizin adınıza RezerveApp sistemi üzerinde bir çalışan hesabı oluşturulmuştur. Sisteme girmek için kullanacağınız giriş bilgileriniz:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <p style='color: #333; font-size: 16px;'><strong>E-posta / Kullanıcı Adı:</strong> {normalizedEmail}</p>
                                <p style='color: #333; font-size: 16px;'><strong>Şifre:</strong></p>
                                <span style='font-size: 24px; font-weight: bold; letter-spacing: 2px; color: #b78a28; padding: 10px 20px; background: #f9f9f9; border-radius: 8px;'>{generatedPassword}</span>
                            </div>
                            <p style='color: #999; font-size: 12px; text-align: center;'>Şifrenizi dilediğiniz zaman profilinizden değiştirebilirsiniz.</p>
                        </div>";
                    await _emailSender.SendEmailAsync(normalizedEmail, "RezerveApp Hesap Bilgileriniz", htmlMessage);
                    TempData["Success"] = $"Çalışan eklendi ve hesap bilgileri e-posta adresine gönderildi.";
                }
                else
                {
                    TempData["Error"] = "Çalışan oluşturuldu ancak kullanıcı hesabı oluşturulamadı: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
                }
            }
            else

            {
                TempData["Success"] = "Çalışan başarıyla oluşturuldu.";
            }

            return RedirectToAction(nameof(Employees));
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId);

            if (employee == null)
                return NotFound();
                
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            ViewBag.EmployeeEmail = user?.Email ?? "";

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, string name, bool isActive, string? email)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Çalışan adı zorunludur.");
                return View(employee);
            }

            // Pasiften aktife geçerken de paket limiti kontrol edilir.
            if (isActive && !employee.IsActive)
            {
                var (canAdd, maxEmployees, _) = await _subscriptionService.CanAddEmployeeAsync(businessId.Value);

                if (!canAdd)
                {
                    ModelState.AddModelError("", $"Paketiniz en fazla {maxEmployees} aktif çalışana izin veriyor.");
                    return View(employee);
                }
            }

            employee.Name = name.Trim();
            employee.IsActive = isActive;

            await _context.SaveChangesAsync();

            // E-posta düzenleme/ekleme
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (TempData["OwnerOtpVerified"] as string != "true")
                {
                    TempData["Error"] = "Çalışan giriş bilgilerini düzenlemek için doğrulama gereklidir.";
                    return RedirectToAction(nameof(Employees));
                }
                TempData.Remove("OwnerOtpVerified");

                bool isValidFormat = false;
                string normalizedEmail = email.Trim().ToLowerInvariant();
                isValidFormat = true;
                
                var emailUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail && u.EmployeeId != id);
                if (emailUser != null)
                {
                    TempData["Error"] = "Girdiğiniz e-posta adresi başka birine ait. Diğer bilgiler güncellendi.";
                    isValidFormat = false; // Block further execution
                }

                if (isValidFormat)
                {
                    if (user == null)
                    {
                        // Yeni kullanıcı oluştur
                        var random = new Random();
                        var generatedPassword = random.Next(100000, 999999).ToString();
                        user = new ApplicationUser
                        {
                            UserName = normalizedEmail,
                            Email = normalizedEmail,
                            EmailConfirmed = true,
                            BusinessId = businessId.Value,
                            EmployeeId = employee.Id
                        };
                        var createResult = await _userManager.CreateAsync(user, generatedPassword);
                        if (createResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "Employee");
                            
                            var htmlMessage = $@"
                                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                                    <h2 style='color: #1a1a1a; text-align: center;'>RezerveApp Hesabınız Açıldı</h2>
                                    <p style='color: #666; font-size: 16px;'>Merhaba,</p>
                                    <p style='color: #333; font-size: 16px;'><strong>Şifre:</strong> {generatedPassword}</p>
                                </div>";
                            await _emailSender.SendEmailAsync(normalizedEmail, "RezerveApp Hesap Bilgileriniz", htmlMessage);
                            TempData["Success"] = $"Çalışan güncellendi ve şifresi E-posta olarak gönderildi.";
                        }
                    }
                    else if (user.UserName != normalizedEmail)
                    {
                        // Mevcut kullanıcının giriş bilgisini güncelle
                        user.Email = normalizedEmail;
                        user.UserName = normalizedEmail;
                        await _userManager.UpdateAsync(user);
                        TempData["Success"] = "Çalışan bilgileri ve giriş adresi güncellendi.";
                    }
                    else 
                    {
                        TempData["Success"] = "Çalışan bilgileri güncellendi.";
                    }
                }
            }
            else
            {
                TempData["Success"] = "Çalışan bilgileri güncellendi.";
            }

            return RedirectToAction(nameof(Employees));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetEmployeePassword(int employeeId, string newPassword)
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId);
            if (employee == null) return NotFound();

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == employee.Id);
            if (user == null)
            {
                TempData["Error"] = "Bu çalışanın bir kullanıcı hesabı bulunamadı.";
                return RedirectToAction("Employees");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Şifre en az 6 karakter olmalıdır.";
                return RedirectToAction("Employees");
            }

            if (TempData["OwnerOtpVerified"] as string != "true")
            {
                TempData["Error"] = "Çalışanın şifresini sıfırlamak için doğrulama gereklidir.";
                return RedirectToAction("Employees");
            }
            TempData.Remove("OwnerOtpVerified");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                var htmlMessage = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h2 style='color: #1a1a1a; text-align: center;'>RezerveApp Şifreniz Sıfırlandı</h2>
                        <p style='color: #666; font-size: 16px;'>Merhaba,</p>
                        <p style='color: #666; font-size: 16px;'>İşletme yöneticiniz tarafından şifreniz sıfırlandı. Yeni giriş bilgileriniz:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <p style='color: #333; font-size: 16px;'><strong>Şifre:</strong></p>
                            <span style='font-size: 24px; font-weight: bold; letter-spacing: 2px; color: #b78a28; padding: 10px 20px; background: #f9f9f9; border-radius: 8px;'>{newPassword}</span>
                        </div>
                    </div>";
                await _emailSender.SendEmailAsync(user.Email, "RezerveApp Şifre Sıfırlama", htmlMessage);
                TempData["Success"] = "Çalışanın şifresi başarıyla sıfırlandı ve e-posta ile gönderildi.";
            }
            else
            {
                TempData["Error"] = "Şifre sıfırlanırken bir hata oluştu: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Employees");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);
            if (customer == null) return NotFound();

            var appointments = await _context.Appointments.Where(a => a.CustomerId == id).ToListAsync();
            _context.Appointments.RemoveRange(appointments);
            
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Müşteri ve ilişkili randevuları kalıcı olarak silindi.";
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlacklist(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);
            if (customer == null) return NotFound();

            customer.IsBlacklisted = !customer.IsBlacklisted;
            await _context.SaveChangesAsync();

            TempData["Success"] = customer.IsBlacklisted 
                ? $"{customer.Name} isimli müşteri başarıyla kara listeye alındı. Artık randevu oluşturamayacak."
                : $"{customer.Name} isimli müşteri kara listeden çıkarıldı.";
                
            return RedirectToAction(nameof(Customers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var notif = await _context.NotificationLogs.FirstOrDefaultAsync(n => n.Id == id && n.BusinessId == businessId);
            if (notif == null) return NotFound();

            _context.NotificationLogs.Remove(notif);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bildirim silindi.";
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == id && e.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            var appointments = await _context.Appointments
                .Where(a => a.EmployeeId == id)
                .ToListAsync();

            var hasActiveAppointments = appointments.Any(a => a.Status != "Cancelled" && a.Status != "NoShow");

            if (hasActiveAppointments)
            {
                // Geçmiş geçerli randevu verisini bozmamak için kalıcı silme yerine pasifleştir.
                employee.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["Error"] = "Bu çalışanın tamamlanmış veya aktif randevu geçmişi olduğu için kalıcı silinmedi, " +
                    "onun yerine pasif duruma (gizli) alındı. İsterseniz iptal edip tekrar silebilirsiniz.";

                return RedirectToAction(nameof(Employees));
            }
            
            // Eğer sadece iptal edilmiş randevuları varsa, o randevuları da silip çalışanı kalıcı silebiliriz.
            if (appointments.Any())
            {
                _context.Appointments.RemoveRange(appointments);
            }

            _context.EmployeeServices.RemoveRange(_context.EmployeeServices.Where(es => es.EmployeeId == id));
            _context.WorkingHours.RemoveRange(_context.WorkingHours.Where(w => w.EmployeeId == id));
            
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.EmployeeId == id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            _context.Employees.Remove(employee);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees));
        }

        // Çalışanın yapabildiği hizmetleri belirleme
        [HttpGet]
        public async Task<IActionResult> EmployeeServices(int employeeId)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            var allServices = await _context.Services
                .Where(s => s.BusinessId == businessId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var assignedServiceIds = await _context.EmployeeServices
                .Where(es => es.EmployeeId == employeeId)
                .Select(es => es.ServiceId)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.AssignedServiceIds = assignedServiceIds;

            return View(allServices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEmployeeServices(int employeeId, int[] serviceIds)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            // Sadece bu işletmeye ait hizmetler kabul edilir (izolasyon).
            var validServiceIds = await _context.Services
                .Where(s => s.BusinessId == businessId && serviceIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            var existing = _context.EmployeeServices.Where(es => es.EmployeeId == employeeId);
            _context.EmployeeServices.RemoveRange(existing);

            foreach (var serviceId in validServiceIds)
            {
                _context.EmployeeServices.Add(new EmployeeService
                {
                    EmployeeId = employeeId,
                    ServiceId = serviceId
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees));
        }

        // =====================================================================
        // ÇALIŞAN ÇALIŞMA SAATLERİ / MOLA / İZİN
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> EmployeeWorkingHours(int employeeId)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            var hours = await _context.WorkingHours
                .Where(x => x.EmployeeId == employeeId)
                .OrderBy(x => x.DayOfWeek)
                .ToListAsync();

            var breaks = await _context.EmployeeBreaks
                .Where(x => x.EmployeeId == employeeId)
                .OrderBy(x => x.DayOfWeek)
                .ToListAsync();

            var timeOffs = await _context.EmployeeTimeOffs
                .Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.Breaks = breaks;
            ViewBag.TimeOffs = timeOffs;

            return View(hours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkingHour(
            int employeeId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, bool isWorking)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            var existingHour = await _context.WorkingHours
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.DayOfWeek == dayOfWeek);

            if (existingHour != null)
            {
                existingHour.StartTime = startTime;
                existingHour.EndTime = endTime;
                existingHour.IsWorking = isWorking;
            }
            else
            {
                _context.WorkingHours.Add(new WorkingHour
                {
                    EmployeeId = employeeId,
                    DayOfWeek = dayOfWeek,
                    StartTime = startTime,
                    EndTime = endTime,
                    IsWorking = isWorking
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployeeBreak(
            int employeeId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            if (endTime <= startTime)
            {
                TempData["Error"] = "Mola bitiş saati başlangıçtan sonra olmalıdır.";
                return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
            }

            _context.EmployeeBreaks.Add(new EmployeeBreak
            {
                EmployeeId = employeeId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployeeBreak(int id, int employeeId)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var brk = await _context.EmployeeBreaks
                .Include(b => b.Employee)
                .FirstOrDefaultAsync(b => b.Id == id && b.Employee!.BusinessId == businessId);

            if (brk != null)
            {
                _context.EmployeeBreaks.Remove(brk);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmployeeTimeOff(
            int employeeId, DateTime startDate, DateTime endDate, string? reason)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == businessId);

            if (employee == null)
                return NotFound();

            if (endDate.Date < startDate.Date)
            {
                TempData["Error"] = "İzin bitiş tarihi başlangıçtan önce olamaz.";
                return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
            }

            _context.EmployeeTimeOffs.Add(new EmployeeTimeOff
            {
                EmployeeId = employeeId,
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                Reason = reason
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployeeTimeOff(int id, int employeeId)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var timeOff = await _context.EmployeeTimeOffs
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Id == id && t.Employee!.BusinessId == businessId);

            if (timeOff != null)
            {
                _context.EmployeeTimeOffs.Remove(timeOff);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(EmployeeWorkingHours), new { employeeId });
        }

        // =====================================================================
        // İŞLETME ÇALIŞMA SAATLERİ (genel açılış/kapanış)
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> WorkingHours()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var hours = await _context.BusinessWorkingHours
                .Where(x => x.BusinessId == businessId)
                .OrderBy(x => x.DayOfWeek)
                .ToListAsync();

            return View(hours);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> SaveWorkingHours(List<BusinessWorkingHourInput> workingHours)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            if (workingHours == null || workingHours.Count == 0)
                return RedirectToAction(nameof(WorkingHours));

            var existing = await _context.BusinessWorkingHours
                .Where(x => x.BusinessId == businessId)
                .ToListAsync();

            for (int i = 0; i < 7 && i < workingHours.Count; i++)
            {
                // Form Pazartesi (index 0) ile başlıyor; .NET DayOfWeek Pazar=0, Pazartesi=1 ...
                var day = (DayOfWeek)((i + 1) % 7);

                var row = workingHours[i];
                var isOpen = row.IsOpen;
                var open = row.OpenTime ?? new TimeSpan(9, 0, 0);
                var close = row.CloseTime ?? new TimeSpan(19, 0, 0);

                var existingRow = existing.FirstOrDefault(x => x.DayOfWeek == day);

                if (existingRow != null)
                {
                    existingRow.IsOpen = isOpen;
                    existingRow.OpenTime = open;
                    existingRow.CloseTime = close;
                }
                else
                {
                    _context.BusinessWorkingHours.Add(new BusinessWorkingHour
                    {
                        BusinessId = businessId.Value,
                        DayOfWeek = day,
                        IsOpen = isOpen,
                        OpenTime = open,
                        CloseTime = close
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Çalışma saatleri güncellendi.";

            return RedirectToAction(nameof(WorkingHours));
        }

        // =====================================================================
        // TATİL GÜNLERİ
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Holidays()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var holidays = await _context.BusinessHolidays
                .Where(x => x.BusinessId == businessId)
                .OrderBy(x => x.Date)
                .ToListAsync();

            return View(holidays);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> AddHoliday(DateTime date, string? reason)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var exists = await _context.BusinessHolidays
                .AnyAsync(h => h.BusinessId == businessId && h.Date.Date == date.Date);

            if (!exists)
            {
                _context.BusinessHolidays.Add(new BusinessHoliday
                {
                    BusinessId = businessId.Value,
                    Date = date.Date,
                    Reason = reason
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Holidays));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var holiday = await _context.BusinessHolidays
                .FirstOrDefaultAsync(h => h.Id == id && h.BusinessId == businessId);

            if (holiday != null)
            {
                _context.BusinessHolidays.Remove(holiday);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Holidays));
        }

        // =====================================================================
        // RANDEVULAR
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Appointments(string? q, string? status, DateTime? date)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var query = _context.Appointments
                .Include(x => x.Employee)
                .Include(x => x.Service)
                .Where(x => x.BusinessId == businessId)
                .AsQueryable();

            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == user.EmployeeId);
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(x =>
                    x.CustomerName.Contains(q) || x.CustomerPhone.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (date.HasValue)
            {
                query = query.Where(x => x.AppointmentDate.Date == date.Value.Date);
            }

            var appointments = await query
                .OrderBy(x => x.AppointmentDate)
                .ThenBy(x => x.StartTime)
                .ToListAsync();

            ViewBag.SearchQuery = q;
            ViewBag.StatusFilter = status;
            ViewBag.DateFilter = date;

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ConfirmAppointment(int id) => UpdateAppointmentStatus(id, "Approved");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> CancelAppointment(int id) => UpdateAppointmentStatus(id, "Cancelled");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, string status)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var query = _context.Appointments.Where(x => x.Id == id && x.BusinessId == businessId);
            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == user.EmployeeId);
                }
            }
            var appointment = await query.FirstOrDefaultAsync();

            if (appointment == null)
                return NotFound();

            if (status != "Approved" && status != "Rejected" && status != "Cancelled" && status != "Completed" && status != "NoShow")
                return BadRequest();

            // Aynı gruba (GroupId) sahip tüm randevuları bul (tekli hizmetse sadece kendisi)
            var groupAppointments = new List<Appointment> { appointment };
            if (!string.IsNullOrEmpty(appointment.GroupId))
            {
                var relatedQuery = _context.Appointments.Where(x => x.GroupId == appointment.GroupId && x.BusinessId == businessId);
                if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user?.EmployeeId != null)
                        relatedQuery = relatedQuery.Where(x => x.EmployeeId == user.EmployeeId);
                }
                groupAppointments = await relatedQuery.ToListAsync();
            }

            foreach(var app in groupAppointments)
            {
                // Eğer durumu NoShow (Gelmedi) yapılıyorsa ve önceki durum NoShow değilse müşteri ceza puanını artır
                // Ceza puanı sadece grup başına 1 kez artsın diye küçük bir kontrol (ancak her app'te artarsa müşteri çok ceza alır, 
                // bu yüzden ilk appointment için ceza verip diğerlerini es geçmek daha mantıklı, ama id'si ilk olana bakalım)
                if (app.Id == groupAppointments.First().Id) 
                {
                    if (status == "NoShow" && app.Status != "NoShow")
                    {
                        var customer = await _context.Customers.FindAsync(app.CustomerId);
                        if (customer != null)
                        {
                            customer.NoShowCount++;
                            if (customer.NoShowCount >= 2)
                                customer.IsBlacklisted = true;
                        }
                    }
                    else if (app.Status == "NoShow" && status != "NoShow")
                    {
                        var customer = await _context.Customers.FindAsync(app.CustomerId);
                        if (customer != null && customer.NoShowCount > 0)
                        {
                            customer.NoShowCount--;
                            if (customer.NoShowCount < 2)
                                customer.IsBlacklisted = false;
                        }
                    }
                }

                app.Status = status;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] = "Bu randevu az önce başka bir işlem tarafından güncellendi. Sayfayı yenileyip tekrar deneyin.";
            }

            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var query = _context.Appointments.Where(x => x.Id == id && x.BusinessId == businessId);
            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == user.EmployeeId);
                }
            }
            var appointment = await query.FirstOrDefaultAsync();

            if (appointment == null)
                return NotFound();

            // Aynı gruba (GroupId) sahip tüm randevuları bul (tekli hizmetse sadece kendisi)
            var groupAppointments = new List<Appointment> { appointment };
            if (!string.IsNullOrEmpty(appointment.GroupId))
            {
                var relatedQuery = _context.Appointments.Where(x => x.GroupId == appointment.GroupId && x.BusinessId == businessId);
                if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user?.EmployeeId != null)
                        relatedQuery = relatedQuery.Where(x => x.EmployeeId == user.EmployeeId);
                }
                groupAppointments = await relatedQuery.ToListAsync();
            }

            // Kalıcı silme işlemi
            _context.Appointments.RemoveRange(groupAppointments);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu kalıcı olarak silindi.";
            return RedirectToAction(nameof(Appointments));
        }

        // Admin panelinden müşteri adına hızlı randevu oluşturma
        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var empQuery = _context.Employees.Where(e => e.BusinessId == businessId && e.IsActive);
            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    empQuery = empQuery.Where(e => e.Id == user.EmployeeId);
                }
            }
            ViewBag.Employees = await empQuery.OrderBy(e => e.Name).ToListAsync();

            ViewBag.Services = await _context.Services
                .Where(s => s.BusinessId == businessId)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(
            string customerName,
            string customerPhone,
            int employeeId,
            int serviceId,
            DateTime appointmentDate,
            TimeSpan startTime)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            async Task<IActionResult> Fail(string message)
            {
                ModelState.AddModelError("", message);
                TempData["Error"] = message;

                var empQueryFail = _context.Employees.Where(e => e.BusinessId == businessId && e.IsActive);
                if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user?.EmployeeId != null)
                    {
                        empQueryFail = empQueryFail.Where(e => e.Id == user.EmployeeId);
                    }
                }
                ViewBag.Employees = await empQueryFail.OrderBy(e => e.Name).ToListAsync();

                ViewBag.Services = await _context.Services
                    .Where(s => s.BusinessId == businessId)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                return View();
            }

            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null && user.EmployeeId != employeeId)
                {
                    return await Fail("Yetkiniz dışındaki bir çalışana randevu oluşturamazsınız.");
                }
            }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerPhone))
                return await Fail("Müşteri adı ve telefonu zorunludur.");

            if (appointmentDate.Date < DateTime.Today)
                return await Fail("Geçmiş bir tarihe randevu oluşturulamaz.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.BusinessId == businessId && e.IsActive);

            var service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessId == businessId);

            if (employee == null || service == null)
                return await Fail("Seçilen çalışan veya hizmet geçersiz.");

            // Çalışan bu hizmeti verebiliyor mu? (o çalışan için hiç kayıt yoksa kısıtlama uygulanmaz)
            var hasAnyServiceRestriction = await _context.EmployeeServices.AnyAsync(es => es.EmployeeId == employeeId);
            if (hasAnyServiceRestriction)
            {
                var canPerform = await _context.EmployeeServices
                    .AnyAsync(es => es.EmployeeId == employeeId && es.ServiceId == serviceId);

                if (!canPerform)
                    return await Fail("Seçilen çalışan bu hizmeti vermiyor.");
            }

            // İzinli mi?
            var onLeave = await _context.EmployeeTimeOffs.AnyAsync(t =>
                t.EmployeeId == employeeId &&
                appointmentDate.Date >= t.StartDate.Date &&
                appointmentDate.Date <= t.EndDate.Date);

            if (onLeave)
                return await Fail("Çalışan seçilen tarihte izinli.");

            // İşletme tatil mi?
            var isHoliday = await _context.BusinessHolidays.AnyAsync(h =>
                h.BusinessId == businessId && h.Date.Date == appointmentDate.Date);

            if (isHoliday)
                return await Fail("Seçilen tarih işletme için tatil günü.");

            var endTime = startTime + TimeSpan.FromMinutes(service.DurationMinutes);

            // Çalışanın o gün çalışma saatleri
            var workingHour = await _context.WorkingHours.FirstOrDefaultAsync(w =>
                w.EmployeeId == employeeId && w.DayOfWeek == appointmentDate.DayOfWeek);

            if (workingHour == null)
            {
                return await Fail("Bu çalışanın seçilen gün (haftanın bu günü) için tanımlanmış bir çalışma saati yok.");
            }

            if (!workingHour.IsWorking)
            {
                return await Fail("Çalışan seçilen gün (haftanın bu günü) kapalı/çalışmıyor.");
            }

            if (startTime < workingHour.StartTime || endTime > workingHour.EndTime)
            {
                return await Fail($"Seçilen saat, çalışanın çalışma saatleri ({workingHour.StartTime:hh\\:mm} - {workingHour.EndTime:hh\\:mm}) dışında.");
            }

            // Mola çakışması
            var onBreak = await _context.EmployeeBreaks.AnyAsync(b =>
                b.EmployeeId == employeeId && b.DayOfWeek == appointmentDate.DayOfWeek &&
                startTime < b.EndTime && endTime > b.StartTime);

            if (onBreak)
                return await Fail("Seçilen saat çalışanın molasına denk geliyor.");

            // Double-booking kontrolü
            var overlapping = await _context.Appointments.AnyAsync(a =>
                a.EmployeeId == employeeId &&
                a.AppointmentDate.Date == appointmentDate.Date &&
                a.Status != "Cancelled" && a.Status != "Rejected" &&
                startTime < a.EndTime && endTime > a.StartTime);

            if (overlapping)
                return await Fail("Bu saat aralığı çalışanın başka bir randevusuyla çakışıyor.");

            // Müşteriyi telefon numarasıyla eşleştir / oluştur
            var customer = await _context.Customers.FirstOrDefaultAsync(c =>
                c.BusinessId == businessId && c.Phone == customerPhone);

            if (customer == null)
            {
                customer = new Customer
                {
                    BusinessId = businessId.Value,
                    Name = customerName.Trim(),
                    Phone = customerPhone.Trim()
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var appointment = new Appointment
            {
                BusinessId = businessId.Value,
                EmployeeId = employeeId,
                ServiceId = serviceId,
                CustomerId = customer.Id,
                CustomerName = customerName.Trim(),
                CustomerPhone = customerPhone.Trim(),
                AppointmentDate = appointmentDate.Date,
                StartTime = startTime,
                EndTime = endTime,
                Status = "Approved"
            };

            try
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return await Fail("Randevu kaydedilirken bir hata oluştu, lütfen tekrar deneyin.");
            }

            try
            {
                await _notificationService.NotifyCustomerConfirmationAsync(appointment);
            }
            catch
            {
                // Bildirim hatası randevu oluşturmayı etkilemez.
            }

            TempData["Success"] = "Randevu oluşturuldu.";

            return RedirectToAction(nameof(Appointments));
        }

        // =====================================================================
        // TAKVİM (günlük / haftalık / aylık, sürükle-bırak)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Calendar(string view = "day", DateTime? date = null)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var empQuery = _context.Employees.Where(e => e.BusinessId == businessId && e.IsActive);
            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    empQuery = empQuery.Where(e => e.Id == user.EmployeeId);
                }
            }
            var employees = await empQuery.OrderBy(e => e.Name).ToListAsync();

            ViewBag.Employees = employees;
            ViewBag.View = view is "day" or "week" or "month" ? view : "day";
            ViewBag.Date = (date ?? DateTime.Today).Date;

            return View();
        }

        // Takvimin belirli bir tarih aralığındaki randevuları JSON olarak çeker.
        [HttpGet]
        public async Task<IActionResult> CalendarData(DateTime start, DateTime end)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var query = _context.Appointments
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Where(a => a.BusinessId == businessId &&
                            a.AppointmentDate.Date >= start.Date &&
                            a.AppointmentDate.Date <= end.Date &&
                            a.Status != "Cancelled" && a.Status != "Rejected")
                .AsQueryable();

            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == user.EmployeeId);
                }
            }

            var appointmentsRaw = await query
                .OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime)
                .Select(a => new
                {
                    id = a.Id,
                    customerName = a.CustomerName,
                    serviceName = a.Service!.Name,
                    durationMinutes = a.Service.DurationMinutes,
                    employeeId = a.EmployeeId,
                    employeeName = a.Employee!.Name,
                    date = a.AppointmentDate,
                    startTime = a.StartTime,
                    endTime = a.EndTime,
                    status = a.Status,
                    rowVersion = a.RowVersion
                })
                .ToListAsync();

            var appointments = appointmentsRaw
                .Select(a => new
                {
                    a.id,
                    a.customerName,
                    a.serviceName,
                    a.durationMinutes,
                    a.employeeId,
                    a.employeeName,
                    date = a.date.ToString("yyyy-MM-dd"),
                    startTime = a.startTime.ToString(@"hh\:mm"),
                    endTime = a.endTime.ToString(@"hh\:mm"),
                    a.status,
                    rowVersion = a.rowVersion == null
                        ? null
                        : Convert.ToBase64String(a.rowVersion)
                })
                .ToList();

            return Json(appointments);
        }

        // Sürükle-bırak ile randevu saati/çalışanı değiştirme.
        // Değişiklik yapılırken tüm müsaitlik kuralları tekrar uygulanır.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveAppointment(
            int id, DateTime newDate, string newStartTime, int newEmployeeId, string? rowVersion)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Json(new { success = false, message = "Yetkisiz." });

            if (!TimeSpan.TryParse(newStartTime, out var parsedStartTime))
                return Json(new { success = false, message = "Geçersiz saat formatı." });

            var query = _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .Where(a => a.Id == id && a.BusinessId == businessId);

            if (User.IsInRole("Employee") && !User.IsInRole("BusinessAdmin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == user.EmployeeId);
                    if (newEmployeeId != user.EmployeeId)
                        return Json(new { success = false, message = "Randevuyu başka bir çalışana taşıyamazsınız." });
                }
            }

            var appointment = await query.FirstOrDefaultAsync();

            if (appointment == null)
                return Json(new { success = false, message = "Randevu bulunamadı." });

            if (appointment.Status == "Cancelled" || appointment.Status == "Rejected" || appointment.Status == "Completed")
                return Json(new { success = false, message = "Bu durumda olan bir randevu taşınamaz." });

            var newEmployee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == newEmployeeId && e.BusinessId == businessId && e.IsActive);

            if (newEmployee == null)
                return Json(new { success = false, message = "Geçersiz çalışan." });

            var validationError = await _availabilityService.ValidateSlotAsync(
                businessId.Value,
                newEmployeeId,
                appointment.Service!.DurationMinutes,
                newDate,
                parsedStartTime,
                excludeAppointmentId: appointment.Id);

            if (validationError != null)
                return Json(new { success = false, message = validationError });

            // Eşzamanlılık: form üzerinden gelen RowVersion, veritabanındaki
            // ile eşleşmiyorsa (randevu bu sırada başka biri tarafından
            // değiştirilmişse) güncelleme reddedilir.
            if (!string.IsNullOrEmpty(rowVersion))
            {
                _context.Entry(appointment).Property(a => a.RowVersion).OriginalValue = Convert.FromBase64String(rowVersion);
            }

            appointment.AppointmentDate = newDate.Date;
            appointment.StartTime = parsedStartTime;
            appointment.EndTime = parsedStartTime.Add(TimeSpan.FromMinutes(appointment.Service.DurationMinutes));
            appointment.EmployeeId = newEmployeeId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Json(new { success = false, message = "Bu randevu az önce başka bir işlem tarafından değiştirildi. Sayfayı yenileyin." });
            }

            return Json(new
            {
                success = true,
                appointment = new
                {
                    id = appointment.Id,
                    date = appointment.AppointmentDate.ToString("yyyy-MM-dd"),
                    startTime = appointment.StartTime.ToString("hh\\:mm"),
                    endTime = appointment.EndTime.ToString("hh\\:mm"),
                    employeeId = appointment.EmployeeId,
                    employeeName = newEmployee.Name,
                    rowVersion = appointment.RowVersion == null ? null : Convert.ToBase64String(appointment.RowVersion)
                }
            });
        }

        // =====================================================================
        // BİLDİRİM GEÇMİŞİ (WhatsApp)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var logs = await _context.NotificationLogs
                .Where(n => n.BusinessId == businessId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            var whatsAppEnabled = _configuration.GetValue<bool>("WhatsApp:Enabled");
            ViewBag.WhatsAppEnabled = whatsAppEnabled;

            return View(logs);
        }

        // =====================================================================
        // MÜŞTERİLER
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> Customers(string? q)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var query = _context.Customers.Where(c => c.BusinessId == businessId);

            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(c => c.Name.Contains(q) || c.Phone.Contains(q));
            }

            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            ViewBag.SearchQuery = q;

            return View(customers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCustomer(int id, string name, string phone)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

            if (customer == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Müşteri adı zorunludur.";
                return RedirectToAction(nameof(CustomerDetails), new { id });
            }

            var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());

            if (normalizedPhone.Length != 11)
            {
                TempData["Error"] = "Telefon numarası 11 haneli olmalıdır.";
                return RedirectToAction(nameof(CustomerDetails), new { id });
            }

            var phoneTaken = await _context.Customers.AnyAsync(c =>
                c.BusinessId == businessId && c.Phone == normalizedPhone && c.Id != id);

            if (phoneTaken)
            {
                TempData["Error"] = "Bu telefon numarasıyla kayıtlı başka bir müşteri var.";
                return RedirectToAction(nameof(CustomerDetails), new { id });
            }

            customer.Name = name.Trim();
            customer.Phone = normalizedPhone;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(CustomerDetails), new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessAdmin")]
        public async Task<IActionResult> ResetBlacklist(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

            if (customer == null)
                return NotFound();

            customer.NoShowCount = 0;
            customer.IsBlacklisted = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Müşterinin kara listesi sıfırlandı.";

            return RedirectToAction(nameof(CustomerDetails), new { id });
        }

        // =====================================================================
        // İŞLETME PROFİLİ
        // =====================================================================
        [HttpGet]
        // =====================================================================
        // SAVE PROFILE
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(int id, string name, string phone, string instagram, string city, string district, string address, string neighborhood, IFormFile? logo, IFormFile? gallery1, IFormFile? gallery2, IFormFile? gallery3, string? latitude, string? longitude, int slotInterval = 30)
        {
            var currentBusinessId = await GetCurrentBusinessIdAsync();
            if (currentBusinessId == null || currentBusinessId != id)
                return Unauthorized();

            var business = await _context.Businesses.FindAsync(id);
            if (business == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                TempData["ErrorMessage"] = "İşletme adı ve telefon zorunludur.";
                return RedirectToAction(nameof(Profile));
            }

            phone = phone.Trim();
            
            if (await _context.Businesses.AnyAsync(b => b.Phone == phone && b.Id != id))
            {
                TempData["ErrorMessage"] = "Bu telefon numarası başka bir işletme tarafından kullanılıyor.";
                return RedirectToAction(nameof(Profile));
            }

            double? parsedLat = null;
            double? parsedLng = null;

            if (!string.IsNullOrWhiteSpace(latitude) && double.TryParse(latitude.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                parsedLat = lat;
                
            if (!string.IsNullOrWhiteSpace(longitude) && double.TryParse(longitude.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                parsedLng = lng;

            business.Name = name;
            business.Phone = phone;
            business.Instagram = instagram;
            business.City = city;
            business.District = district;
            business.Address = address ?? "";
            business.Neighborhood = neighborhood;
            business.Latitude = parsedLat;
            business.Longitude = parsedLng;
            
            // Eğer boş/hatalı değer gelirse koruma amaçlı 30 yap, en az 10 dakika olsun.
            business.SlotInterval = slotInterval < 10 ? 30 : slotInterval;

            if (logo != null && logo.Length > 0)
            {
                if (logo.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Logo dosyası 5MB'dan küçük olmalıdır.";
                    return RedirectToAction(nameof(Profile));
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Sadece JPG, JPEG ve PNG formatları kabul edilmektedir.";
                    return RedirectToAction(nameof(Profile));
                }

                try
                {
                    var uploadedUrl = await _imageService.UploadImageAsync(logo);
                    if (!string.IsNullOrEmpty(uploadedUrl))
                    {
                        business.LogoUrl = uploadedUrl;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Logo yüklenemedi. Sunucu ayarlarınızı (Cloudinary) kontrol edin.";
                    }
                }
                catch (Exception ex) 
                { 
                    TempData["ErrorMessage"] = "Logo yüklenirken hata: " + ex.Message;
                }
            }

            if (gallery1 != null && gallery1.Length > 0)
            {
                try { 
                    var g1 = await _imageService.UploadImageAsync(gallery1); 
                    if (!string.IsNullOrEmpty(g1)) business.GalleryImage1 = g1;
                } catch { }
            }
            
            if (gallery2 != null && gallery2.Length > 0)
            {
                try { 
                    var g2 = await _imageService.UploadImageAsync(gallery2); 
                    if (!string.IsNullOrEmpty(g2)) business.GalleryImage2 = g2;
                } catch { }
            }
            
            if (gallery3 != null && gallery3.Length > 0)
            {
                try { 
                    var g3 = await _imageService.UploadImageAsync(gallery3); 
                    if (!string.IsNullOrEmpty(g3)) business.GalleryImage3 = g3;
                } catch { }
            }

            // Password Change Logic
            var user = await _userManager.GetUserAsync(User);
            if (user != null && Request.Form.ContainsKey("currentPassword") && Request.Form.ContainsKey("newPassword"))
            {
                var currentPassword = Request.Form["currentPassword"].ToString();
                var newPassword = Request.Form["newPassword"].ToString();
                var confirmNewPassword = Request.Form["confirmNewPassword"].ToString();
                
                if (!string.IsNullOrEmpty(currentPassword) && !string.IsNullOrEmpty(newPassword))
                {
                    if (newPassword != confirmNewPassword)
                    {
                        TempData["ErrorMessage"] = "Yeni şifreler eşleşmiyor.";
                        return RedirectToAction(nameof(Profile));
                    }
                    var changeResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                    if (!changeResult.Succeeded)
                    {
                        TempData["ErrorMessage"] = "Şifre değiştirilemedi. Mevcut şifreniz hatalı olabilir.";
                        return RedirectToAction(nameof(Profile));
                    }
                }
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Profil başarıyla güncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Profile()
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId.Value);

            if (business == null)
                return NotFound();

            ViewBag.BusinessName = business.Name;
            ViewBag.PageTitle = "İşletmem";
            ViewBag.ActiveNav = "Profile";

            return View(business);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            var businessId = await GetCurrentBusinessIdAsync();

            if (businessId == null)
                return Unauthorized();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id && c.BusinessId == businessId);

            if (customer == null)
                return NotFound();

            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Employee)
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var totalCount = appointments.Count;
            var noShowCount = appointments.Count(a => a.Status == "NoShow");

            var completedOrApproved = appointments
                .Where(a => a.Status == "Approved" || a.Status == "Completed")
                .ToList();

            var favoriteService = appointments
                .Where(a => a.Status != "Cancelled" && a.Status != "Rejected")
                .GroupBy(a => a.Service!.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            // Aldığı hizmetler: benzersiz hizmet adı + kaç kez alındığı
            var receivedServices = appointments
                .Where(a => a.Status != "Cancelled" && a.Status != "Rejected")
                .GroupBy(a => a.Service!.Name)
                .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            var totalSpent = completedOrApproved.Sum(a => a.Service!.Price);

            ViewBag.Customer = customer;
            ViewBag.TotalAppointments = totalCount;
            ViewBag.NoShowCount = noShowCount;
            ViewBag.FavoriteService = favoriteService;
            ViewBag.ReceivedServices = receivedServices;
            ViewBag.TotalSpent = totalSpent;

            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueChartData(string period = "monthly", int offset = 0)
        {
            try {
            var businessId = await GetCurrentBusinessIdAsync();
            if (businessId == null) return Unauthorized();

            var today = DateTime.Today;
            var labels = new List<string>();
            var data = new List<decimal>();
            string title = "";
            string subtitle = "";

            if (period == "daily")
            {
                var targetDate = today.AddDays(offset * 7); // shift by weeks
                var startDate = targetDate.AddDays(-6);
                title = "Günlük Ciro Eğilimi";
                subtitle = $"{startDate:dd MMM} - {targetDate:dd MMM} aralığındaki gelir";

                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate.Date && x.AppointmentDate <= targetDate.Date.AddDays(1))
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => x.AppointmentDate.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();

                for (int i = 6; i >= 0; i--)
                {
                    var d = targetDate.AddDays(-i).Date;
                    labels.Add(d.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));
                    var dayData = query.FirstOrDefault(x => x.Date == d);
                    data.Add(dayData?.Revenue ?? 0m);
                }
            }
            else if (period == "yearly")
            {
                var targetYear = today.Year + (offset * 5); // shift by 5 years
                var startYear = targetYear - 4;
                title = "Yıllık Ciro Eğilimi";
                subtitle = $"{startYear} - {targetYear} yılları arasındaki gelir";

                var startDate = new DateTime(startYear, 1, 1);
                var endDate = new DateTime(targetYear, 12, 31, 23, 59, 59);

                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => x.AppointmentDate.Year)
                    .Select(g => new { Year = g.Key, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();

                for (int i = 4; i >= 0; i--)
                {
                    int y = targetYear - i;
                    labels.Add(y.ToString());
                    var yearData = query.FirstOrDefault(x => x.Year == y);
                    data.Add(yearData?.Revenue ?? 0m);
                }
            }
            else // monthly
            {
                var targetDate = today.AddMonths(offset * 6); // shift by 6 months
                var startDate = new DateTime(targetDate.Year, targetDate.Month, 1).AddMonths(-5);
                var endDate = new DateTime(targetDate.Year, targetDate.Month, DateTime.DaysInMonth(targetDate.Year, targetDate.Month), 23, 59, 59);
                
                title = "Aylık Ciro Eğilimi";
                subtitle = $"{startDate:MMM yyyy} - {targetDate:MMM yyyy} aralığındaki gelir";

                var rawData = await _context.Appointments
                    .Include(x => x.Service)
                    .Where(x => x.BusinessId == businessId &&
                                (x.Status == "Approved" || x.Status == "Completed") &&
                                x.AppointmentDate >= startDate && x.AppointmentDate <= endDate)
                    .ToListAsync();

                var query = rawData
                    .GroupBy(x => new { x.AppointmentDate.Year, x.AppointmentDate.Month })
                    .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Revenue = g.Sum(a => a.Service?.Price ?? 0m) })
                    .ToList();

                for (int i = 5; i >= 0; i--)
                {
                    var d = targetDate.AddMonths(-i);
                    labels.Add(d.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")));
                    var m = query.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                    data.Add(m?.Revenue ?? 0m);
                }
            }

            return Json(new { labels, data, title, subtitle });
            } catch (Exception ex) {
                System.IO.File.WriteAllText("chart_err.txt", ex.ToString());
                return StatusCode(500, ex.Message);
            }
        }

        // =====================================================================
        // BUSINESS OWNER OTP (SECURITY)
        // =====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOwnerOtp()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return Json(new { success = false, message = "Sistemde kayıtlı e-posta adresiniz bulunamadı." });
            
            var success = await _otpService.GenerateAndSendOtpAsync(user.Email, "BusinessOwnerAction");
            if (success) 
            {
                var maskedEmail = user.Email.Length > 4 ? user.Email.Substring(0, 3) + "***@" + user.Email.Split('@')[1] : user.Email;
                return Json(new { success = true, phone = maskedEmail }); 
            }
            return Json(new { success = false, message = "E-posta gönderilemedi. Lütfen daha sonra tekrar deneyin." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOwnerOtp(string code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return Json(new { success = false });

            var isOtpValid = await _otpService.VerifyOtpAsync(user.Email, code, "BusinessOwnerAction");
            if (!isOtpValid) return Json(new { success = false, message = "Geçersiz veya süresi dolmuş kod." });

            TempData["OwnerOtpVerified"] = "true";
            return Json(new { success = true });
        }
    }
}

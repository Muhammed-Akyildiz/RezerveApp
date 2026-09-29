using RezerveApp.Data;
using RezerveApp.Models;
using RezerveApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionService _subscriptionService;
        private readonly IWebHostEnvironment _env;
        private readonly ISettingsService _settingsService;

        private static readonly string[] AllowedLogoContentTypes =
        {
            "image/png",
            "image/jpeg",
            "image/webp"
        };

        private static readonly string[] AllowedLogoExtensions =
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp"
        };

        private const long MaxLogoSizeBytes = 2 * 1024 * 1024;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SubscriptionService subscriptionService,
            IWebHostEnvironment env,
            ISettingsService settingsService)
        {
            _context = context;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
            _env = env;
            _settingsService = settingsService;
        }

        // =========================================================
        // LOGO KAYDET
        // =========================================================

        private async Task<(string? Url, string? Error)> SaveLogoAsync(
            IFormFile? logoFile)
        {
            if (logoFile == null || logoFile.Length == 0)
                return (null, null);

            if (logoFile.Length > MaxLogoSizeBytes)
                return (null, "Logo dosyası 2MB'tan büyük olamaz.");

            if (!AllowedLogoContentTypes.Contains(logoFile.ContentType))
            {
                return (
                    null,
                    "Logo yalnızca PNG, JPG veya WEBP formatında olabilir."
                );
            }

            var extension =
                Path.GetExtension(logoFile.FileName)
                    .ToLowerInvariant();

            if (!AllowedLogoExtensions.Contains(extension))
                return (null, "Geçersiz dosya uzantısı.");

            var uploadsFolder =
                Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "logos"
                );

            Directory.CreateDirectory(uploadsFolder);

            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName
                );

            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            return (
                $"/uploads/logos/{fileName}",
                null
            );
        }

        // =========================================================
        // ANA SAYFA
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var businesses = await _context.Businesses
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var totalUsers = await _userManager.Users.CountAsync(u => u.BusinessId == null && u.EmployeeId == null);
            var totalAppointments = await _context.Appointments.CountAsync();
            var thisMonthAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate.Month == DateTime.UtcNow.Month && a.AppointmentDate.Year == DateTime.UtcNow.Year)
                .CountAsync();
            
            var pendingSupportTickets = await _context.NotificationLogs
                .Where(n => n.RecipientPhone == "SUPPORT")
                .CountAsync();

            ViewBag.TotalBusinesses = businesses.Count;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalAppointments = totalAppointments;
            ViewBag.ThisMonthAppointments = thisMonthAppointments;
            ViewBag.PendingSupportTickets = pendingSupportTickets;

            return View(businesses);
        }

        // =========================================================
        // FATURA VE ABONELİKLER
        // =========================================================

        public async Task<IActionResult> Billing()
        {
            var subscriptions = await _context.BusinessSubscriptions
                .Include(bs => bs.Business)
                .Include(bs => bs.SubscriptionPlan)
                .Where(bs => bs.IsActive)
                .OrderByDescending(bs => bs.EndDate)
                .ToListAsync();

            return View(subscriptions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubscriptionDates(int id, DateTime startDate, DateTime endDate)
        {
            var sub = await _context.BusinessSubscriptions.FindAsync(id);
            if (sub == null)
            {
                TempData["Error"] = "Abonelik bulunamadı.";
                return RedirectToAction(nameof(Billing));
            }

            sub.StartDate = startDate;
            sub.EndDate = endDate;
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Abonelik tarihleri başarıyla güncellendi.";
            return RedirectToAction(nameof(Billing));
        }

        // =========================================================
        // İŞLETME DETAY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var business =
                await _context.Businesses
                    .FirstOrDefaultAsync(
                        b => b.Id == id);

            if (business == null)
                return NotFound();

            var employees =
                await _context.Employees
                    .Where(e =>
                        e.BusinessId == id)
                    .OrderBy(e => e.Name)
                    .ToListAsync();

            var services =
                await _context.Services
                    .Where(s =>
                        s.BusinessId == id)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

            var appointments =
                await _context.Appointments
                    .Where(a =>
                        a.BusinessId == id)
                    .OrderByDescending(
                        a => a.AppointmentDate)
                    .ThenByDescending(
                        a => a.StartTime)
                    .Take(50)
                    .ToListAsync();

            var activeSubscription =
                await _subscriptionService
                    .GetActiveSubscriptionAsync(id);

            var plans =
                await _context.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Type)
                    .ToListAsync();

            var businessAdminUsers = await _userManager.Users.Where(u => u.BusinessId == id && u.EmployeeId == null).ToListAsync();
            var businessAdmin = businessAdminUsers.FirstOrDefault(u => _userManager.IsInRoleAsync(u, "BusinessAdmin").Result);

            var vm =
                new BusinessDetailsViewModel
                {
                    Business = business,
                    BusinessAdminUser = businessAdmin,
                    Employees = employees,
                    Services = services,
                    Appointments = appointments,
                    ActiveSubscription =
                        activeSubscription,
                    AvailablePlans = plans
                };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBusinessOwnerCredentials(int businessId, string newPhone, string newPassword)
        {
            var businessAdminUsers = await _userManager.Users.Where(u => u.BusinessId == businessId && u.EmployeeId == null).ToListAsync();
            var businessAdmin = businessAdminUsers.FirstOrDefault(u => _userManager.IsInRoleAsync(u, "BusinessAdmin").Result);

            if (businessAdmin == null)
            {
                TempData["Error"] = "Bu işletmeye ait yönetici hesabı bulunamadı.";
                return RedirectToAction(nameof(Details), new { id = businessId });
            }

            bool hasChanges = false;
            
            if (!string.IsNullOrWhiteSpace(newPhone) && newPhone != businessAdmin.PhoneNumber)
            {
                var normalizedPhone = new string(newPhone.Where(char.IsDigit).ToArray());
                if (normalizedPhone.StartsWith("90") && normalizedPhone.Length == 12)
                    normalizedPhone = "0" + normalizedPhone.Substring(2);
                else if (!normalizedPhone.StartsWith("0") && normalizedPhone.Length == 10)
                    normalizedPhone = "0" + normalizedPhone;

                var existingPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone && u.Id != businessAdmin.Id);
                if (existingPhone != null)
                {
                    TempData["Error"] = "Bu telefon numarası başka bir kullanıcı tarafından kullanılıyor.";
                    return RedirectToAction(nameof(Details), new { id = businessId });
                }

                businessAdmin.PhoneNumber = normalizedPhone;
                var updateResult = await _userManager.UpdateAsync(businessAdmin);
                if (!updateResult.Succeeded)
                {
                    TempData["Error"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id = businessId });
                }
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(businessAdmin);
                var resetResult = await _userManager.ResetPasswordAsync(businessAdmin, token, newPassword);
                if (!resetResult.Succeeded)
                {
                    TempData["Error"] = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Details), new { id = businessId });
                }
                hasChanges = true;
            }

            if (hasChanges)
                TempData["Success"] = "İşletme sahibi bilgileri başarıyla güncellendi.";
            else
                TempData["Error"] = "Değişiklik yapılmadı.";

            return RedirectToAction(nameof(Details), new { id = businessId });
        }

        // =========================================================
        // İŞLETME DÜZENLEME
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var business = await _context.Businesses.FindAsync(id);

            if (business == null)
                return NotFound();

            var owner = await _userManager.Users.FirstOrDefaultAsync(u => u.BusinessId == id && u.EmployeeId == null);
            ViewBag.AdminEmail = owner?.Email;

            return View(business);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Business model,
            IFormFile? logoFile,
            string? AdminEmail,
            string? AdminPassword)
        {
            if (id != model.Id)
                return BadRequest();

            var business = await _context.Businesses.FindAsync(id);

            if (business == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.Status = business.Status;
                model.IsActive = business.IsActive;
                model.CreatedAt = business.CreatedAt;
                model.LogoUrl = business.LogoUrl;
                
                var owner = await _userManager.Users.FirstOrDefaultAsync(u => u.BusinessId == id && u.EmployeeId == null);
                ViewBag.AdminEmail = owner?.Email;

                return View(model);
            }

            var (logoUrl, logoError) = await SaveLogoAsync(logoFile);

            if (logoError != null)
            {
                ModelState.AddModelError("", logoError);

                model.Status = business.Status;
                model.IsActive = business.IsActive;
                model.CreatedAt = business.CreatedAt;
                model.LogoUrl = business.LogoUrl;
                
                var owner = await _userManager.Users.FirstOrDefaultAsync(u => u.BusinessId == id && u.EmployeeId == null);
                ViewBag.AdminEmail = owner?.Email;

                return View(model);
            }

            // Update Business details
            business.Name = model.Name;
            business.Phone = model.Phone;
            business.Slug = model.Slug;
            business.Email = model.Email;
            business.Address = model.Address;
            business.City = model.City;
            business.District = model.District;
            business.Neighborhood = model.Neighborhood;
            business.Latitude = model.Latitude;
            business.Longitude = model.Longitude;

            if (logoUrl != null)
            {
                business.LogoUrl = logoUrl;
            }

            // Update Admin Credentials
            var adminUser = await _userManager.Users.FirstOrDefaultAsync(u => u.BusinessId == id && u.EmployeeId == null);
            if (adminUser != null)
            {
                if (!string.IsNullOrEmpty(AdminEmail) && adminUser.Email != AdminEmail)
                {
                    await _userManager.SetEmailAsync(adminUser, AdminEmail);
                    await _userManager.SetUserNameAsync(adminUser, AdminEmail);
                }

                if (!string.IsNullOrEmpty(AdminPassword))
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var result = await _userManager.ResetPasswordAsync(adminUser, resetToken, AdminPassword);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        
                        var ownerErr = await _userManager.Users.FirstOrDefaultAsync(u => u.BusinessId == id && u.EmployeeId == null);
                        ViewBag.AdminEmail = ownerErr?.Email;
                        return View(model);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "İşletme başarıyla güncellendi.";
            return RedirectToAction(nameof(Details), new { id = business.Id });
        }

        // =========================================================
        // ÖDEME ALINDI
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaymentReceived(
            int businessId, DateTime? newEndDate)
        {
            var business =
                await _context.Businesses
                    .FindAsync(businessId);

            if (business == null)
                return NotFound();

            var subscription =
                await _subscriptionService
                    .GetActiveSubscriptionAsync(businessId);

            if (subscription == null)
            {
                TempData["Error"] =
                    "Ödeme kaydetmek için önce işletmeye bir paket atanmalıdır.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = businessId });
            }

            subscription.PaymentStatus =
                PaymentStatus.Paid;

            subscription.PaymentDate =
                DateTime.UtcNow;

            if (newEndDate.HasValue)
            {
                subscription.EndDate = newEndDate.Value;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Ödeme alındı olarak işaretlendi.";

            return RedirectToAction(
                nameof(Details),
                new { id = businessId });
        }

        // =========================================================
        // ONAYLA
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var business =
                await _context.Businesses
                    .FindAsync(id);

            if (business == null)
                return NotFound();

            var subscription =
                await _subscriptionService
                    .GetActiveSubscriptionAsync(id);

            if (subscription == null)
            {
                TempData["Error"] =
                    "İşletmeyi onaylamak için önce bir paket atanmalı ve ödeme alınmalıdır.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (subscription.PaymentStatus !=
                PaymentStatus.Paid)
            {
                TempData["Error"] =
                    "İşletmeyi onaylamak için önce ödemenin alınması gerekiyor.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            business.Status =
                BusinessStatus.Approved;

            business.IsActive = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "İşletme başarıyla onaylandı.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // REDDET
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var business =
                await _context.Businesses
                    .FindAsync(id);

            if (business == null)
                return NotFound();

            business.Status =
                BusinessStatus.Rejected;

            business.IsActive = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // ASKIYA AL
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id)
        {
            var business =
                await _context.Businesses
                    .FindAsync(id);

            if (business == null)
                return NotFound();

            business.Status =
                BusinessStatus.Suspended;

            business.IsActive = false;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // AKTİFLEŞTİR
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var business =
                await _context.Businesses
                    .FindAsync(id);

            if (business == null)
                return NotFound();

            var subscription =
                await _subscriptionService
                    .GetActiveSubscriptionAsync(id);

            if (subscription == null ||
                subscription.PaymentStatus != PaymentStatus.Paid)
            {
                TempData["Error"] =
                    "İşletmeyi aktifleştirmek için geçerli paket ve alınmış ödeme gereklidir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            business.Status =
                BusinessStatus.Approved;

            business.IsActive = true;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // =========================================================
        // İŞLETME SİL
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var business =
                    await _context.Businesses
                        .FindAsync(id);

                if (business == null)
                    return NotFound();

                var users =
                    _userManager.Users
                        .Where(u =>
                            u.BusinessId == id)
                        .ToList();

                foreach (var user in users)
                {
                    await _userManager
                        .DeleteAsync(user);
                }

                _context.Businesses
                    .Remove(business);

                await _context.SaveChangesAsync();

                await transaction
                    .CommitAsync();

                return RedirectToAction(
                    nameof(Index));
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                TempData["Error"] =
                    "İşletme silinirken bir hata oluştu. " +
                    "İlişkili kayıtlar (randevu, hizmet vb.) olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }

        // =========================================================
        // ABONELİK / PAKET ATAMA
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSubscription(
            int businessId,
            int subscriptionPlanId,
            BillingPeriod billingPeriod)
        {
            var business =
                await _context.Businesses
                    .FindAsync(businessId);

            if (business == null)
                return NotFound();

            var plan =
                await _context.SubscriptionPlans
                    .FindAsync(subscriptionPlanId);

            if (plan == null)
            {
                TempData["Error"] =
                    "Seçilen paket bulunamadı.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = businessId });
            }

            // =====================================================
            // PAKET DÜŞÜRME KONTROLÜ
            // =====================================================

            if (plan.MaxEmployees != null)
            {
                var activeEmployees =
                    await _context.Employees
                        .Where(e =>
                            e.BusinessId == businessId &&
                            e.IsActive)
                        .ToListAsync();

                if (activeEmployees.Count >
                    plan.MaxEmployees.Value)
                {
                    foreach (var employee
                             in activeEmployees)
                    {
                        employee.IsActive = false;
                    }
                }
            }

            // =====================================================
            // YENİ PAKETİ ATA
            // =====================================================

            await _subscriptionService
                .AssignPlanAsync(
                    businessId,
                    subscriptionPlanId,
                    billingPeriod);

            return RedirectToAction(
                nameof(Details),
                new { id = businessId });
        }

        // =========================================================
        // İŞLETME OLUŞTUR
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Business business,
            string adminEmail,
            string adminPassword,
            IFormFile? logoFile)
        {
            if (!ModelState.IsValid)
                return View(business);

            var (logoUrl, logoError) =
                await SaveLogoAsync(logoFile);

            if (logoError != null)
            {
                ModelState.AddModelError(
                    "",
                    logoError
                );

                return View(business);
            }

            using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                business.Status =
                    BusinessStatus.Approved;

                business.IsActive = true;

                business.CreatedAt =
                    DateTime.UtcNow;

                business.Email =
                    adminEmail;

                if (logoUrl != null)
                    business.LogoUrl =
                        logoUrl;

                // İşletmeyi oluştur
                _context.Businesses
                    .Add(business);

                await _context
                    .SaveChangesAsync();

                // =================================================
                // BUSINESS ADMIN
                // =================================================

                var user =
                    new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        BusinessId =
                            business.Id
                    };

                var result =
                    await _userManager
                        .CreateAsync(
                            user,
                            adminPassword);

                if (!result.Succeeded)
                {
                    foreach (var error
                             in result.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    await transaction
                        .RollbackAsync();

                    return View(business);
                }

                // =================================================
                // ROL
                // =================================================

                var roleResult =
                    await _userManager
                        .AddToRoleAsync(
                            user,
                            "BusinessAdmin");

                if (!roleResult.Succeeded)
                {
                    foreach (var error
                             in roleResult.Errors)
                    {
                        ModelState.AddModelError(
                            "",
                            error.Description);
                    }

                    await transaction
                        .RollbackAsync();

                    return View(business);
                }

                await transaction
                    .CommitAsync();

                return RedirectToAction(
                    nameof(Index));
            }
            catch
            {
                await transaction
                    .RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "İşletme oluşturulurken bir hata oluştu.");

                return View(business);
            }
        }

        // =========================================================
        // DESTEK TALEPLERİ
        // =========================================================

        public async Task<IActionResult> SupportTickets()
        {
            // Mesaj tipi destek olanları veya RecipientPhone = "SUPPORT" olanları getir
            var tickets = await _context.NotificationLogs
                .Include(n => n.Business)
                .Where(n => n.RecipientPhone == "SUPPORT")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.NotificationLogs.FindAsync(id);
            if (ticket == null || ticket.RecipientPhone != "SUPPORT")
            {
                return NotFound();
            }

            // Talebi silerek veya çözüldü işaretleyerek listeden kaldırabiliriz.
            // Şimdilik silmeyi tercih ediyoruz.
            _context.NotificationLogs.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Destek talebi başarıyla kapatıldı (silindi).";
            return RedirectToAction(nameof(SupportTickets));
        }

        // =========================================================
        // MÜŞTERİ (SON KULLANICI) YÖNETİMİ
        // =========================================================

        public async Task<IActionResult> Users()
        {
            // İşletme sahibi veya çalışan olmayan, sadece normal müşteri olan kullanıcıları getir
            // Ayrıca SuperAdmin olanları da hariç tutmak iyi olur (şimdi basitleştirmek için Role üzerinden değil, BusinessId ve EmployeeId üzerinden gidiyoruz)
            var users = await _userManager.Users
                .Where(u => u.BusinessId == null && u.EmployeeId == null)
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Eğer zaten kilitliyse kilidi aç, değilse sonsuza kadar kilitle
            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = "Kullanıcının engeli kaldırıldı.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = "Kullanıcı başarıyla engellendi.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            
            TempData["SuccessMessage"] = "Kullanıcı başarıyla silindi.";
            return RedirectToAction(nameof(Users));
        }

        // =========================================================
        // SYSTEM SETTINGS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings.ToListAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(string key, string value, string description)
        {
            await _settingsService.SetSettingAsync(key, value, description);
            TempData["SuccessMessage"] = "Ayar güncellendi.";
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSetting(string key)
        {
            await _settingsService.RemoveSettingAsync(key);
            TempData["SuccessMessage"] = "Ayar silindi.";
            return RedirectToAction(nameof(Settings));
        }

        // =========================================================
        // SUBSCRIPTION PLANS CRUD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> SubscriptionPlans()
        {
            var plans = await _context.SubscriptionPlans.OrderBy(p => p.MonthlyPrice).ToListAsync();
            return View(plans);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePlan(SubscriptionPlan model)
        {
            if (ModelState.IsValid)
            {
                _context.SubscriptionPlans.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Paket başarıyla eklendi.";
            }
            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePlan(SubscriptionPlan model)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(model.Id);
            if (plan != null && ModelState.IsValid)
            {
                plan.Name = model.Name;
                plan.Type = model.Type;
                plan.MaxEmployees = model.MaxEmployees;
                plan.MaxServices = model.MaxServices;
                plan.MonthlyPrice = model.MonthlyPrice;
                plan.YearlyPrice = model.YearlyPrice;
                plan.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Paket başarıyla güncellendi.";
            }
            return RedirectToAction(nameof(SubscriptionPlans));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                // Soft delete ya da deactivate yapmak daha güvenli
                plan.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Paket pasife alındı.";
            }
            return RedirectToAction(nameof(SubscriptionPlans));
        }

        // =========================================================
        // NOTIFICATION TEMPLATES CRUD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Templates()
        {
            var templates = await _context.NotificationTemplates.ToListAsync();
            return View(templates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTemplate(NotificationTemplate model)
        {
            if (ModelState.IsValid)
            {
                _context.NotificationTemplates.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Şablon başarıyla eklendi.";
            }
            return RedirectToAction(nameof(Templates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTemplate(NotificationTemplate model)
        {
            var template = await _context.NotificationTemplates.FindAsync(model.Id);
            if (template != null && ModelState.IsValid)
            {
                template.Type = model.Type;
                template.Language = model.Language;
                template.Content = model.Content;
                template.IsActive = model.IsActive;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Şablon güncellendi.";
            }
            return RedirectToAction(nameof(Templates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template != null)
            {
                _context.NotificationTemplates.Remove(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Şablon silindi.";
            }
            return RedirectToAction(nameof(Templates));
        }
    }
}
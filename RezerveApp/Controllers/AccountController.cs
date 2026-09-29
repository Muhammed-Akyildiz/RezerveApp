using RezerveApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RezerveApp.Services.IEmailSender _emailSender;
        private readonly RezerveApp.Services.IOtpService _otpService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RezerveApp.Services.IEmailSender emailSender,
            RezerveApp.Services.IOtpService otpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _otpService = otpService;
        }

        // =========================
        // LOGIN - GET
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }


        // =========================
        // LOGIN - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "E-posta/Telefon ve şifre zorunludur.");
                return View();
            }

            var input = email.Trim();
            ApplicationUser user = null;

            // Check if input is email or phone
            if (input.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(input);
            }
            else
            {
                // Format phone number
                var normalizedPhone = new string(input.Where(char.IsDigit).ToArray());
                // First try finding by username (which we will set to phone for new employees)
                user = await _userManager.FindByNameAsync(normalizedPhone);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Hesap bulunamadı veya şifre hatalı.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                password,
                false,
                false);

            if (result.Succeeded)
            {
                // SuperAdmin
                if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                // BusinessAdmin
                if (await _userManager.IsInRoleAsync(user, "BusinessAdmin"))
                {
                    return RedirectToAction("Index", "Business");
                }

                // Employee
                if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    return RedirectToAction("Appointments", "Business");
                }

                // Rol bulunamadıysa çıkış yap
                await _signInManager.SignOutAsync();

                ModelState.AddModelError("", "Kullanıcı rolü bulunamadı.");
                return View();
            }

            ModelState.AddModelError("", "Hesap bulunamadı veya şifre hatalı.");
            return View();
        }


        // =========================
        // REGISTER - GET
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        // =========================
        // REGISTER - POST
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRegistrationOtp(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "Telefon numarası gereklidir." });

            var success = await _otpService.GenerateAndSendOtpAsync(phone, "Register");
            if (success)
            {
                return Json(new { success = true, message = "Doğrulama kodu gönderildi." });
            }
            return Json(new { success = false, message = "Kod gönderilirken bir hata oluştu." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string email,
            string phone,
            string password,
            string confirmPassword,
            string otpCode)
        {
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                ModelState.AddModelError("", "Doğrulama kodu zorunludur.");
                return View();
            }

            var isOtpValid = await _otpService.VerifyOtpAsync(phone, otpCode, "Register");
            if (!isOtpValid)
            {
                ModelState.AddModelError("", "Doğrulama kodu hatalı veya süresi dolmuş.");
                return View();
            }
            // E-posta kontrolü
            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "",
                    "E-posta adresi zorunludur.");

                return View();
            }

            email = email.Trim().ToLowerInvariant();

            // Şifre kontrolü
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(
                    "",
                    "Şifre zorunludur.");

                return View();
            }

            // Şifre tekrar kontrolü
            if (password != confirmPassword)
            {
                ModelState.AddModelError(
                    "",
                    "Şifreler eşleşmiyor.");

                return View();
            }

            // E-posta daha önce kullanılmış mı?
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "",
                    "Bu e-posta adresi zaten kayıtlı. Lütfen giriş yapın.");

                return View();
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError("", "Telefon numarası zorunludur.");
                return View();
            }

            var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
            
            if (normalizedPhone.StartsWith("90") && normalizedPhone.Length == 12)
            {
                normalizedPhone = "0" + normalizedPhone.Substring(2);
            }
            else if (!normalizedPhone.StartsWith("0") && normalizedPhone.Length == 10)
            {
                normalizedPhone = "0" + normalizedPhone;
            }

            if (normalizedPhone.Length != 11)
            {
                ModelState.AddModelError("phone", "Telefon numarası geçersiz (başında 0 ile 11 hane olmalıdır).");
                return View();
            }

            // Telefon daha önce kullanılmış mı?
            var phoneUser = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (phoneUser != null)
            {
                ModelState.AddModelError("", "Bu telefon numarası zaten kayıtlı.");
                return View();
            }

            // Yeni kullanıcı
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                PhoneNumber = normalizedPhone
            };

            // Kullanıcı oluştur
            var result = await _userManager.CreateAsync(
                user,
                password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            // BusinessAdmin rolünü ver
            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "BusinessAdmin");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            // Otomatik giriş
            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            // Telefon ve e-postayı TempData ile Business/Create sayfasına aktar
            TempData["RegisteredPhone"] = normalizedPhone;
            TempData["RegisteredEmail"] = email;

            // İşletme oluşturma sayfasına gönder
            return RedirectToAction(
                "Create",
                "Business");
        }


        // =========================
        // SETTINGS
        // =========================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Email = user.Email;
            ViewBag.Phone = user.PhoneNumber;

            // Layout belirleme
            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                ViewBag.Layout = "_AdminLayout";
            }
            else
            {
                ViewBag.Layout = "_BusinessLayout";
            }

            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(string email, string phone, string currentPassword, string newPassword, string confirmNewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                ViewBag.Layout = "_AdminLayout";
            }
            else
            {
                ViewBag.Layout = "_BusinessLayout";
            }

            ViewBag.Email = email;
            ViewBag.Phone = phone;

            bool hasChanges = false;

            // E-posta güncelleme
            if (!string.IsNullOrWhiteSpace(email) && email != user.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(email);
                if (emailExists != null && emailExists.Id != user.Id)
                {
                    ModelState.AddModelError("", "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
                    return View();
                }
                
                user.Email = email;
                user.UserName = email;
                hasChanges = true;
            }

            // Telefon güncelleme
            if (phone != user.PhoneNumber)
            {
                user.PhoneNumber = phone;
                hasChanges = true;
            }

            if (hasChanges)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View();
                }
            }

            // Şifre güncelleme
            if (!string.IsNullOrWhiteSpace(currentPassword) || !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmNewPassword))
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    ModelState.AddModelError("", "Şifrenizi değiştirmek için mevcut şifrenizi girmelisiniz.");
                    return View();
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    ModelState.AddModelError("", "Yeni şifre boş olamaz.");
                    return View();
                }

                if (newPassword != confirmNewPassword)
                {
                    ModelState.AddModelError("", "Yeni şifre ile tekrarı uyuşmuyor.");
                    return View();
                }

                var passwordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View();
                }
            }

            if (hasChanges || !string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["SuccessMessage"] = "Hesap ayarlarınız başarıyla güncellendi.";
                await _signInManager.RefreshSignInAsync(user);
            }

            return RedirectToAction("Settings");
        }


        // =========================
        // LOGOUT
        // =========================

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }
        // =========================
        // FORGOT PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Lütfen e-posta adresinizi girin.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // Kullanıcı yoksa da başarılı gibi gösterelim ki e-postaların var olup olmadığı anlaşılamasın (Güvenlik)
                ViewBag.Message = "Eğer sistemimizde kayıtlı bir e-posta adresi girdiyseniz, şifre sıfırlama bağlantısı gönderilmiştir.";
                return View();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "Şifrenizi Sıfırlayın",
                $"Lütfen şifrenizi sıfırlamak için şu linke tıklayın: <a href='{callbackUrl}'>Şifremi Sıfırla</a>");

            ViewBag.Message = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
            return View();
        }

        // =========================
        // RESET PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return BadRequest("Şifre sıfırlama kodu geçersiz.");
            }
            return View(new { Code = code });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string password, string confirmPassword, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(code))
            {
                ViewBag.Error = "Tüm alanları doldurunuz.";
                return View(new { Code = code });
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View(new { Code = code });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "Geçersiz istek.";
                return View(new { Code = code });
            }

            var result = await _userManager.ResetPasswordAsync(user, code, password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(new { Code = code });
        }

        // =========================
        // OTP PASSWORD RESET
        // =========================
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPasswordResetOtp(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "Telefon numarası gereklidir." });

            var normalizedPhone = phone.Replace(" ", "");
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                // Güvenlik için kullanıcı yoksa da "gönderildi" diyebiliriz veya hata dönebiliriz.
                return Json(new { success = false, message = "Bu telefon numarasıyla kayıtlı bir hesap bulunamadı." });
            }

            var success = await _otpService.GenerateAndSendOtpAsync(phone, "PasswordReset");
            if (success)
            {
                return Json(new { success = true, message = "Doğrulama kodu gönderildi." });
            }
            return Json(new { success = false, message = "Kod gönderilirken bir hata oluştu." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPasswordResetOtp(string phone, string code)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Eksik bilgi." });

            var isOtpValid = await _otpService.VerifyOtpAsync(phone, code, "PasswordReset");
            if (!isOtpValid)
            {
                return Json(new { success = false, message = "Doğrulama kodu hatalı veya süresi dolmuş." });
            }

            var normalizedPhone = phone.Replace(" ", "");
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            return Json(new { success = true, token = resetToken });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordWithToken(string phone, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Eksik bilgi." });

            var normalizedPhone = phone.Replace(" ", "");
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Şifre sıfırlanamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)) });
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RezerveApp.Data;
using RezerveApp.Models;
using RezerveApp.Services.Sms;
using System.Collections.Generic;

namespace RezerveApp.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsProvider _smsProvider;
        private readonly IEmailSender _emailSender;
        private readonly IWhatsAppService _whatsAppService;

        public OtpService(ApplicationDbContext context, ISmsProvider smsProvider, IEmailSender emailSender, IWhatsAppService whatsAppService)
        {
            _context = context;
            _smsProvider = smsProvider;
            _emailSender = emailSender;
            _whatsAppService = whatsAppService;
        }

        public async Task<bool> GenerateAndSendOtpAsync(string phoneNumber, string purpose)
        {
            var normalizedPhone = NormalizePhone(phoneNumber);
            
            // Invalidate any existing unused OTPs for this phone and purpose
            var existingOtps = await _context.OtpCodes
                .Where(o => o.PhoneNumber == normalizedPhone && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();
            
            foreach (var otp in existingOtps)
            {
                otp.IsUsed = true;
            }

            var random = new Random();
            var code = random.Next(100000, 999999).ToString();

            var otpCode = new OtpCode
            {
                PhoneNumber = normalizedPhone,
                Code = code,
                ExpirationTime = DateTime.UtcNow.AddMinutes(3), // 3 minutes validity
                Purpose = purpose,
                IsUsed = false
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            var message = $"RezerveApp doğrulama kodunuz: {code}. Güvenliğiniz için bu kodu kimseyle paylaşmayınız.";

            if (normalizedPhone.Contains("@"))
            {
                var htmlMessage = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                        <h2 style='color: #1a1a1a; text-align: center;'>RezerveApp Doğrulama Kodu</h2>
                        <p style='color: #666; font-size: 16px;'>Merhaba,</p>
                        <p style='color: #666; font-size: 16px;'>İşleminizi tamamlamak için doğrulama kodunuz aşağıdadır:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #b78a28; padding: 10px 20px; background: #f9f9f9; border-radius: 8px;'>{code}</span>
                        </div>
                        <p style='color: #999; font-size: 12px; text-align: center;'>Güvenliğiniz için bu kodu kimseyle paylaşmayınız. Kodun geçerlilik süresi 3 dakikadır.</p>
                    </div>";

                await _emailSender.SendEmailAsync(normalizedPhone, "RezerveApp Doğrulama Kodu", htmlMessage);
                return true;
            }
            else
            {
                // Format phone number to E164 for WhatsApp
                var e164 = normalizedPhone;
                if (!e164.StartsWith("+"))
                {
                    if (e164.StartsWith("0")) e164 = "+90" + e164.Substring(1);
                    else if (e164.Length == 10) e164 = "+90" + e164;
                    else e164 = "+" + e164;
                }

                // WhatsApp template sending
                var waResult = await _whatsAppService.SendTemplateMessageAsync(e164, "dogrulama_kodu", new List<string> { code });
                
                // Fallback to SMS if WhatsApp fails
                if (!waResult.Success)
                {
                    return await _smsProvider.SendSmsAsync(normalizedPhone, message);
                }
                
                return true;
            }
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string code, string purpose)
        {
            var normalizedPhone = NormalizePhone(phoneNumber);

            var otpRecord = await _context.OtpCodes
                .Where(o => o.PhoneNumber == normalizedPhone && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.ExpirationTime)
                .FirstOrDefaultAsync();

            if (otpRecord == null) return false;

            // Mark as used regardless of correctness if expired to clean up
            if (otpRecord.ExpirationTime < DateTime.UtcNow) 
            {
                otpRecord.IsUsed = true;
                await _context.SaveChangesAsync();
                return false;
            }

            if (otpRecord.Code != code) return false;

            otpRecord.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

            if (phone.Contains("@")) return phone.Trim().ToLowerInvariant();

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("0"))
                return "90" + digits.Substring(1);

            if (digits.Length == 12 && digits.StartsWith("90"))
                return digits;

            if (digits.Length == 10)
                return "90" + digits;

            return digits;
        }
    }
}

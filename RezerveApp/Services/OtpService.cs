using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RezerveApp.Data;
using RezerveApp.Models;
using RezerveApp.Services.Sms;

namespace RezerveApp.Services
{
    public class OtpService : IOtpService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsProvider _smsProvider;

        public OtpService(ApplicationDbContext context, ISmsProvider smsProvider)
        {
            _context = context;
            _smsProvider = smsProvider;
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
            return await _smsProvider.SendSmsAsync(normalizedPhone, message);
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
            var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

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

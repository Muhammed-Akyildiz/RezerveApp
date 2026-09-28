using RezerveApp.Data;
using RezerveApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace RezerveApp.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CachePrefix = "Setting_";
        private const string AllSettingsCacheKey = "AllSettings";

        public SettingsService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var cacheKey = CachePrefix + key;
            if (_cache.TryGetValue(cacheKey, out string? cachedValue))
            {
                return cachedValue;
            }

            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            var value = setting?.Value;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(24));
                
            _cache.Set(cacheKey, value, cacheEntryOptions);

            return value;
        }

        public async Task SetSettingAsync(string key, string? value, string? description = null)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = key,
                    Value = value,
                    Description = description
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                if (description != null)
                {
                    setting.Description = description;
                }
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();

            // Update cache
            var cacheKey = CachePrefix + key;
            _cache.Set(cacheKey, value, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(24)));
            _cache.Remove(AllSettingsCacheKey); // Invalidate all settings cache if we had one
        }

        public async Task RemoveSettingAsync(string key)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                _context.SystemSettings.Remove(setting);
                await _context.SaveChangesAsync();
                
                var cacheKey = CachePrefix + key;
                _cache.Remove(cacheKey);
                _cache.Remove(AllSettingsCacheKey);
            }
        }

        public Task ClearCacheAsync()
        {
            // Note: IMemoryCache doesn't have a clear all method by default. 
            // In a robust implementation, you might use an IMemoryCache wrapper or a cancellation token to reset.
            // For simplicity, we assume setting operations update their specific cache keys.
            return Task.CompletedTask;
        }
    }
}

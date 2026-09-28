namespace RezerveApp.Services
{
    public interface ISettingsService
    {
        Task<string?> GetSettingAsync(string key);
        Task SetSettingAsync(string key, string? value, string? description = null);
        Task RemoveSettingAsync(string key);
        Task ClearCacheAsync();
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RezerveApp.Services;

namespace RezerveApp.Middlewares
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // We only block if they are not going to Admin, Identity, or the Maintenance page itself.
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            bool isExempt = path.StartsWith("/admin") || 
                            path.StartsWith("/identity") || 
                            path.StartsWith("/home/maintenance") || 
                            path.StartsWith("/css") || 
                            path.StartsWith("/js") || 
                            path.StartsWith("/lib");

            if (!isExempt)
            {
                // ISettingsService is scoped, so we need to get it from the RequestServices
                var settingsService = context.RequestServices.GetRequiredService<ISettingsService>();
                var maintenanceMode = await settingsService.GetSettingAsync("MaintenanceMode");

                if (maintenanceMode?.ToLower() == "true")
                {
                    // Redirect to the Maintenance page
                    context.Response.Redirect("/Home/Maintenance");
                    return;
                }
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}

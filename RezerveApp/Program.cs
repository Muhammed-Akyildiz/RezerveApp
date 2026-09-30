using RezerveApp.Data;
using RezerveApp.HostedServices;
using RezerveApp.Models;
using RezerveApp.Services;
using RezerveApp.Services.Sms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RezerveApp.Middlewares;

var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ======================================================
// DATABASE
// ======================================================

var useInMemoryDb = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemoryDb)
    {
        options.UseInMemoryDatabase("RezerveApp_E2ETestDb");
    }
    else
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (connectionString != null && connectionString.Contains("Host=")) 
        {
            options.UseNpgsql(connectionString);
        }
        else
        {
            options.UseSqlServer(connectionString);
        }
    }
});


// ======================================================
// IDENTITY
// ======================================================

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


// ======================================================
// MVC
// ======================================================

builder.Services.AddControllersWithViews();

builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddTransient<IEmailSender, BrevoEmailSender>();
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppBusinessCloudService>();
builder.Services.AddHttpClient<ISmsProvider, VerimorSmsProvider>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHostedService<AppointmentReminderHostedService>();

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IImageService, LocalImageService>();
builder.Services.AddScoped<IOtpService, OtpService>();


var app = builder.Build();


// ======================================================
// ROLLER VE SUPER ADMIN OLUŞTUR
// ======================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    // Apply any pending migrations automatically
    await dbContext.Database.MigrateAsync();

    // ------------------------------
    // Rolleri oluştur
    // ------------------------------

    if (!await roleManager.RoleExistsAsync("SuperAdmin"))
    {
        await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
    }

    if (!await roleManager.RoleExistsAsync("BusinessAdmin"))
    {
        await roleManager.CreateAsync(new IdentityRole("BusinessAdmin"));
    }

    if (!await roleManager.RoleExistsAsync("Employee"))
    {
        await roleManager.CreateAsync(new IdentityRole("Employee"));
    }

    // ------------------------------
    // Super Admin hesabı
    // ------------------------------

    var adminEmail = "admin@rezerveapp.com";
    var admin = await userManager.FindByEmailAsync(adminEmail);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, "Admin123!");

        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                Console.WriteLine($"SuperAdmin oluşturulamadı: {error.Description}");
            }
        }
    }

    // ------------------------------
    // SuperAdmin rolünü ver
    // ------------------------------

    if (admin != null && !await userManager.IsInRoleAsync(admin, "SuperAdmin"))
    {
        await userManager.AddToRoleAsync(admin, "SuperAdmin");
    }
}


// ======================================================
// HTTP REQUEST PIPELINE
// ======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS
if (!useInMemoryDb)
{
    app.UseHttpsRedirection();
}

// Static files
app.UseStaticFiles();

// Maintenance Middleware
app.UseMiddleware<MaintenanceMiddleware>();

// Routing
app.UseRouting();

// Authentication
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// ======================================================
// ATTRIBUTE ROUTING
// ======================================================
app.MapControllers();

// ======================================================
// NORMAL MVC ROUTING
// ======================================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ======================================================
// START
// ======================================================
app.Run();

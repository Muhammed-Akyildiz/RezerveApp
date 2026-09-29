using RezerveApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RezerveApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Business> Businesses { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<WorkingHour> WorkingHours { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<BusinessSubscription> BusinessSubscriptions { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BusinessWorkingHour> BusinessWorkingHours { get; set; }
        public DbSet<BusinessHoliday> BusinessHolidays { get; set; }
        public DbSet<EmployeeBreak> EmployeeBreaks { get; set; }
        public DbSet<EmployeeTimeOff> EmployeeTimeOffs { get; set; }
        public DbSet<EmployeeService> EmployeeServices { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
        public DbSet<OtpCode> OtpCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SubscriptionPlan>()
                .Property(p => p.MonthlyPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<SubscriptionPlan>()
                .Property(p => p.YearlyPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Business)
                .WithMany()
                .HasForeignKey(a => a.BusinessId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Customer)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Business>()
                .HasIndex(b => b.Slug)
                .IsUnique();

            modelBuilder.Entity<Business>()
                .HasIndex(b => b.Phone)
                .IsUnique();

            modelBuilder.Entity<Business>()
                .HasIndex(b => b.Email)
                .IsUnique()
                .HasFilter("\"Email\" IS NOT NULL");

            modelBuilder.Entity<Customer>()
                .HasIndex(c => new { c.BusinessId, c.Phone })
                .IsUnique();

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.Business)
                .WithMany()
                .HasForeignKey(c => c.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusinessSubscription>()
                .HasOne(bs => bs.Business)
                .WithMany(b => b.Subscriptions)
                .HasForeignKey(bs => bs.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusinessSubscription>()
                .HasOne(bs => bs.SubscriptionPlan)
                .WithMany()
                .HasForeignKey(bs => bs.SubscriptionPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusinessWorkingHour>()
                .HasOne(w => w.Business)
                .WithMany()
                .HasForeignKey(w => w.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusinessHoliday>()
                .HasOne(h => h.Business)
                .WithMany()
                .HasForeignKey(h => h.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeBreak>()
                .HasOne(eb => eb.Employee)
                .WithMany()
                .HasForeignKey(eb => eb.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeTimeOff>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeService>()
                .HasKey(es => new { es.EmployeeId, es.ServiceId });

            modelBuilder.Entity<EmployeeService>()
                .HasOne(es => es.Employee)
                .WithMany()
                .HasForeignKey(es => es.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeService>()
                .HasOne(es => es.Service)
                .WithMany()
                .HasForeignKey(es => es.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NotificationLog>()
                .HasOne(n => n.Appointment)
                .WithMany()
                .HasForeignKey(n => n.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<NotificationLog>()
                .HasOne(n => n.Business)
                .WithMany()
                .HasForeignKey(n => n.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan
                {
                    Id = 1,
                    Name = "Başlangıç",
                    Type = SubscriptionPlanType.Starter,
                    MaxEmployees = 1,
                    MaxServices = 5,
                    MonthlyPrice = 0m,
                    YearlyPrice = 0m,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 2,
                    Name = "Basic",
                    Type = SubscriptionPlanType.Basic,
                    MaxEmployees = 2,
                    MaxServices = null, // Sınırsız
                    MonthlyPrice = 199m,
                    YearlyPrice = 1990m,
                    IsActive = true
                },
                new SubscriptionPlan
                {
                    Id = 3,
                    Name = "Premium",
                    Type = SubscriptionPlanType.Premium,
                    MaxEmployees = null, // Sınırsız
                    MaxServices = null, // Sınırsız
                    MonthlyPrice = 299m,
                    YearlyPrice = 2990m,
                    IsActive = true
                }
            );
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Business)
                .WithMany()
                .HasForeignKey(r => r.BusinessId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

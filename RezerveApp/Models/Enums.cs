namespace RezerveApp.Models
{
    // İşletmenin SuperAdmin tarafından yönetilen durumu
    public enum BusinessStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Suspended = 3
    }

    // Abonelik paketi türü
    public enum SubscriptionPlanType
    {
        Basic = 0,
        Premium = 1,
        Starter = 2
    }

    // Faturalama periyodu
    public enum BillingPeriod
    {
        Monthly = 0,
        Yearly = 1
    }

    // WhatsApp bildirim türü
    public enum NotificationType
    {
        NewAppointmentBusiness = 0,
        AppointmentConfirmationCustomer = 1,
        AppointmentReminderCustomer = 2
    }

}

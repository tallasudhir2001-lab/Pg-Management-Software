using PgManagement_WebApi.Options;
using PgManagement_WebApi.Services;

namespace PgManagement_WebApi;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAdvanceService, AdvanceService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAccessPointDiscoveryService, AccessPointDiscoveryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<IPaymentModeService, PaymentModeService>();
        services.AddScoped<IPaymentTypeService, PaymentTypeService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAccessPointAdminService, AccessPointAdminService>();
        services.AddScoped<IReportSubscriptionService, ReportSubscriptionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IPgUserManagementService, PgUserManagementService>();
        services.AddScoped<IPaymentService, PaymentService>();

        // Email
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        var emailProvider = configuration["Email:Provider"];
        if (emailProvider == "AwsSes")
            services.AddScoped<IEmailProvider, AwsSesEmailProvider>();
        else
            services.AddScoped<IEmailProvider, SmtpEmailProvider>();

        // WhatsApp
        services.Configure<WhatsAppOptions>(configuration.GetSection("WhatsApp"));
        services.AddHttpClient<IWhatsAppProvider, WhatsAppCloudApiProvider>();
        services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();

        return services;
    }
}

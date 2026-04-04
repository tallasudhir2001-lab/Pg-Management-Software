using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PgManagement_WebApi.Data;
using PgManagement_WebApi.Identity;
using PgManagement_WebApi.MiddleWare;
using PgManagement_WebApi.Options;
using PgManagement_WebApi.Services;
using PgManagement_WebApi.Jobs;
using PgManagement_WebApi.Filters;
using Hangfire;
using Hangfire.SqlServer;
using QuestPDF.Infrastructure;
using System.Text;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAdvanceService, AdvanceService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAccessPointDiscoveryService, AccessPointDiscoveryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IPaymentModeService, PaymentModeService>();
builder.Services.AddScoped<IPaymentTypeService, PaymentTypeService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAccessPointAdminService, AccessPointAdminService>();
builder.Services.AddScoped<IReportSubscriptionService, ReportSubscriptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPgUserManagementService, PgUserManagementService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

var emailProvider = builder.Configuration["Email:Provider"];
if (emailProvider == "AwsSes")
    builder.Services.AddScoped<IEmailProvider, AwsSesEmailProvider>();
else
    builder.Services.AddScoped<IEmailProvider, SmtpEmailProvider>();

// WhatsApp
builder.Services.Configure<WhatsAppOptions>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.AddHttpClient<IWhatsAppProvider, WhatsAppCloudApiProvider>();
builder.Services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();

// Hangfire
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();
builder.Services.AddSingleton<DailyReportEmailJob>();
builder.Services.AddSingleton<DailyRentReminderJob>();



builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options=>
{
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._@";
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<PgManagement_WebApi.Filters.AccessPointAuthorizationFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PG Management API",
        Version = "v1"
     });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Put Your Token Here"
     });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ExceptionMiddleWare>();
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Hangfire Dashboard (admin-only)
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

//Seed Inital Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await SeedData.InitializeAsync(context, userManager, roleManager);
}

// Sync access points from controller attributes
using (var scope = app.Services.CreateScope())
{
    var discoveryService = scope.ServiceProvider.GetRequiredService<IAccessPointDiscoveryService>();
    await discoveryService.SyncAccessPointsAsync();
}

// Register Hangfire recurring jobs
RecurringJob.AddOrUpdate<DailyRentReminderJob>(
    "daily-rent-reminders",
    job => job.ExecuteAsync(),
    "0 9 * * *",  // 9:00 AM daily
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time") });

RecurringJob.AddOrUpdate<DailyReportEmailJob>(
    "daily-report-emails",
    job => job.ExecuteAsync(),
    "0 8 * * *",  // 8:00 AM daily
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time") });

app.Run();

using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.BackgroundJobs;
using SMSNotificationSystem.Data;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Repositories;
using SMSNotificationSystem.Repositories.Interfaces;
using SMSNotificationSystem.Services;
using SMSNotificationSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC (anti-forgery token is validated on every POST automatically) ----------
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// ---------- Settings ----------
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection(NotificationOptions.SectionName));

// ---------- Data access (Dapper) ----------
builder.Services.AddSingleton<DapperContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ITriggerRepository, TriggerRepository>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IDeliveryLogRepository, DeliveryLogRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IGatewaySettingRepository, GatewaySettingRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IPayrollRepository, PayrollRepository>();

// ---------- Business services ----------
// The gateway is resolved through an interface: swap SimulatedGatewayService for a real
// Alpha SMS / Twilio / SMTP implementation without touching anything else.
builder.Services.AddScoped<INotificationGateway, SimulatedGatewayService>();
builder.Services.AddScoped<ITriggerService, TriggerService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<TriggerScanJob>();
builder.Services.AddScoped<DispatchJob>();

// ---------- Cookie authentication ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "NotifyHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// ---------- Make sure the database, tables, views, procedures and seed data exist ----------
// (runs SQL/01..05 - the same scripts you can run in SSMS; safe to run every start)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing in appsettings.json.");
if (builder.Configuration.GetValue("Database:AutoSetup", true))
{
    using var loggerFactory = LoggerFactory.Create(l => l.AddConsole());
    DatabaseInitializer.EnsureDatabase(
        connectionString,
        builder.Configuration.GetValue("Database:RecreateIfOutdated", false),
        loggerFactory.CreateLogger("DatabaseInitializer"));
}

// ---------- Hangfire (background jobs stored in the same SQL Server database) ----------
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        PrepareSchemaIfNecessary = true,          // creates the [HangFire] tables on first run
        QueuePollInterval = TimeSpan.FromSeconds(15),
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 2;
    options.ServerName = $"NotifyHub-{Environment.MachineName}";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Hangfire dashboard: Administrators only
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "NotifyHub background jobs",
    Authorization = new[] { new HangfireDashboardAuthFilter() },
    AppPath = "/Dashboard"
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ---------- Recurring jobs: trigger scan + queue dispatch ----------
var notificationSettings = builder.Configuration.GetSection(NotificationOptions.SectionName).Get<NotificationOptions>() ?? new NotificationOptions();

// clean up job ids used by older versions of this project
RecurringJob.RemoveIfExists("process-message-queue");
RecurringJob.RemoveIfExists("evaluate-triggers");

RecurringJob.AddOrUpdate<TriggerScanJob>(
    TriggerScanJob.JobId,
    job => job.RunAsync(),
    notificationSettings.TriggerScanCron);

RecurringJob.AddOrUpdate<DispatchJob>(
    DispatchJob.JobId,
    job => job.RunAsync(),
    notificationSettings.DispatchCron);

app.Run();

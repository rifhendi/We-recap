using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiteAttendance.Data;
using SiteAttendance.Models;
using SiteAttendance.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Resolve PostgreSQL Connection String (Support DATABASE_URL standard)
string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? ConvertDatabaseUrlToNpgsql(databaseUrl)
    : builder.Configuration.GetConnectionString("DefaultConnection") 
      ?? "Host=localhost;Port=5432;Database=site_attendance_db;Username=postgres;Password=postgres";

// 2. Setup Entity Framework Core (PostgreSQL Default / InMemory Preview Support)
bool useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase") 
                   || Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true"
                   || connectionString.Equals("InMemory", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("SiteAttendanceDemoDb");
    }
    else
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    }
});

// 3. Setup ASP.NET Core Identity untuk Autentikasi Administrator
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. Setup Cookie Authentication & Security
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "SiteAttendance.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(10);
    options.SlidingExpiration = true;
});

// 5. Setup Razor Pages & Global Authorization
builder.Services.AddRazorPages(options =>
{
    // Seluruh halaman pengelolaan wajib login administrator
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// 6. Register Application Services (Modular Monolith)
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IDatabaseBackupService, DatabaseBackupService>();

var app = builder.Build();

// 7. Auto-migration & Database Seeding pada Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Memulai inisialisasi database PostgreSQL...");
        await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
        logger.LogInformation("Inisialisasi database PostgreSQL berhasil.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Perhatian: Inisialisasi PostgreSQL belum dapat tersambung. Pastikan PostgreSQL Cloud/Local aktif dengan connection string yang benar.");
    }
}

// 8. HTTP Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();

// Helper untuk konversi DATABASE_URL ke Npgsql format (Heroku/Render/Railway/Supabase/Neon)
static string ConvertDatabaseUrlToNpgsql(string databaseUrl)
{
    if (!databaseUrl.StartsWith("postgres://") && !databaseUrl.StartsWith("postgresql://"))
    {
        return databaseUrl;
    }

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = userInfo.Length > 0 ? userInfo[0] : "",
        Password = userInfo.Length > 1 ? userInfo[1] : "",
        SslMode = SslMode.Prefer
    };

    return builder.ToString();
}

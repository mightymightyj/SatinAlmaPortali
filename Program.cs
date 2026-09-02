using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Hubs;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

var builder = WebApplication.CreateBuilder(args);

// Türkçe karakter desteği için global HTML ve JavaScript Encoder ayarları
builder.Services.AddSingleton<HtmlEncoder>(HtmlEncoder.Create(UnicodeRanges.All));
builder.Services.AddSingleton<JavaScriptEncoder>(JavaScriptEncoder.Create(UnicodeRanges.All));

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    })
    .AddRazorRuntimeCompilation();
builder.Services.AddMemoryCache();

// E-posta Servisi (Gmail SMTP / MailKit)
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));
builder.Services.AddSingleton<EmailService>();

// Cloudinary Görsel Yükleme Servisi
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection(CloudinarySettings.SectionName));
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Entity Framework Core DbContext Kaydı (PostgreSQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TedarikDb")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=satinalmaportali;Username=postgres;Password=postgres;";

    options.UseNpgsql(connectionString);
});

// SignalR Gerçek Zamanlı İletişim Servisi
builder.Services.AddSignalR();

// Dashboard Servis Kaydı
builder.Services.AddScoped<IDashboardService, DashboardService>();

// iyzico Sandbox & Escrow (Havuz) Servis Kaydı
builder.Services.AddScoped<IIyzicoPaymentService, IyzicoPaymentService>();
builder.Services.AddHostedService<EscrowAutoApprovalBackgroundService>();

// Kimlik Doğrulama (Cookie Authentication)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

var app = builder.Build();

// Otomatik Veritabanı Başlangıç Verisi (Seed Data)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedDataAsync(dbContext);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB SEEDING ERROR: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
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

app.MapHub<PortalHub>("/portalHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// SignalR Hub Endpoint
app.MapHub<PortalHub>("/hubs/portal");

app.Run();

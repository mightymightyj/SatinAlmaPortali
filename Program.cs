using Microsoft.EntityFrameworkCore;
using satinalmaportali2.Data;
using satinalmaportali2.Models;
using satinalmaportali2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Entity Framework Core DbContext Kaydı
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TedarikDb")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=TedarikDb;Trusted_Connection=True;MultipleActiveResultSets=true";

    options.UseSqlServer(connectionString);
});

// Dashboard Servis Kaydı
builder.Services.AddScoped<IDashboardService, DashboardService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;
using TruckQueuingDashboard.Application.Interfaces.Repositories;
using TruckQueuingDashboard.Application.Interfaces.Services;
using TruckQueuingDashboard.Application.Services;
using TruckQueuingDashboard.Infrastructure.Data;
using TruckQueuingDashboard.Infrastructure.Hubs;
using TruckQueuingDashboard.Infrastructure.Repositories;
using TruckQueuingDashboard.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Add services to the container ───────────────────────────────
builder.Services.AddControllersWithViews();

// ─── Tell ASP.NET Core where to find views ──────────────────────
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

// ─── Authentication (Cookie) ─────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// ─── Database Context ────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── Repositories & Services ─────────────────────────────────────
builder.Services.AddScoped<IFleetRepository, FleetRepository>();
builder.Services.AddScoped<IFleetService, FleetService>();

// ─── Background Service (File Watcher) ──────────────────────────
builder.Services.AddHostedService<FleetFileWatcherService>();

// ─── SignalR ──────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── Session & HttpContext Accessor ──────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// ─── Configure the HTTP request pipeline ─────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ─── Serve static files from Presentation/wwwroot ───────────────
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Presentation", "wwwroot"))
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();   // ← must be before UseAuthorization
app.UseAuthorization();

// ─── Map static assets (optional) ───────────────────────────────
app.MapStaticAssets();

// ─── Default route ───────────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Dispatcher}/{id?}")
    .WithStaticAssets();

// ─── SignalR Hub ──────────────────────────────────────────────────
app.MapHub<FleetHub>("/fleetHub");

app.Run();
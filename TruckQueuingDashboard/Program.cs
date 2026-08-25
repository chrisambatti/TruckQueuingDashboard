using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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

// ─── 1. Add services to the container ───────────────────────────────
builder.Services.AddControllersWithViews();

// ─── 2. Tell ASP.NET Core where to find views ──────────────────────
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

// ─── 3. Authentication (Cookie) ─────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.LogoutPath = "/Login/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// ─── 4. Authorization (Fallback policy – require authenticated users) ──
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ─── 5. Database Context ────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 6. Repositories & Services ─────────────────────────────────────
builder.Services.AddScoped<IFleetRepository, FleetRepository>();
builder.Services.AddScoped<IFleetService, FleetService>();

// ─── 7. Background Service (File Watcher) ──────────────────────────
builder.Services.AddHostedService<FleetFileWatcherService>();

// ─── 8. SignalR ──────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── 9. Session & HttpContext Accessor ─────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

var app = builder.Build();

// ─── 10. Configure the HTTP request pipeline ────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ─── 11. Serve static files from Presentation/wwwroot ──────────────
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Presentation", "wwwroot"))
});

app.UseRouting();

app.UseSession();
app.UseAuthentication();   // Must be before UseAuthorization
app.UseAuthorization();

// ─── 12. Map static assets (optional) ──────────────────────────────
app.MapStaticAssets();

// ─── 13. Default route – now points to Login/Index ─────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
    .WithStaticAssets();

// ─── 14. SignalR Hub ─────────────────────────────────────────────────
app.MapHub<FleetHub>("/fleetHub");

app.Run();
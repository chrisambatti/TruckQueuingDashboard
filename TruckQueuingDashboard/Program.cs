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
using Serilog;

//var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "Logs", "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine("Presentation", "wwwroot")
});

builder.Host.UseSerilog();

// ─── 1. Register MVC (required for controllers) ────────────────────
builder.Services.AddControllersWithViews();

// ─── 2. Tell ASP.NET Core where to find views ──────────────────────
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

// ─── 3. Database Context ────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 4. Repositories & Services ─────────────────────────────────────
builder.Services.AddScoped<IFleetRepository, FleetRepository>();
builder.Services.AddScoped<IFleetService, FleetService>();

// ─── 5. Background Service (File Watcher) ──────────────────────────
builder.Services.AddHostedService<FleetFileWatcherService>();

// ─── 6. SignalR ──────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── 7. Session & HttpContext Accessor ─────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();

// ─── 8. Authentication / Authorization – REMOVED ───────────────────

var app = builder.Build();

// ─── 9. Configure HTTP pipeline ─────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ─── 10. Serve static files from Presentation/wwwroot ──────────────
//app.UseStaticFiles(new StaticFileOptions
//{
//    FileProvider = new PhysicalFileProvider(
//        Path.Combine(app.Environment.ContentRootPath, "Presentation", "wwwroot"))
//});

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

// ─── 11. Map static assets (optional) ──────────────────────────────
app.MapStaticAssets();

// ─── 12. Default route – change to a public controller ─────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Dispatcher}/{id?}")
    .WithStaticAssets();

// ─── 13. SignalR Hub ─────────────────────────────────────────────────
app.MapHub<FleetHub>("/fleetHub");

app.Run();
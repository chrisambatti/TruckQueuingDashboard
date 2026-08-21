using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TruckQueuingDashboard.Application.Interfaces.Services;
using TruckQueuingDashboard.Domain.Constants;
using TruckQueuingDashboard.Infrastructure.Hubs;

namespace TruckQueuingDashboard.Infrastructure.Services
{
    public class FleetFileWatcherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<FleetHub> _hubContext;
        private readonly string _folderPath;
        private readonly System.Timers.Timer _debounceTimer;
        private string _lastFile = string.Empty;
        private bool _isProcessing;

        public FleetFileWatcherService(
            IServiceScopeFactory scopeFactory,
            IHubContext<FleetHub> hubContext,
            IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _folderPath = configuration["Fleet:FolderPath"] ?? TQConstants.FleetFolderPath;

            _debounceTimer = new System.Timers.Timer(2000);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!Directory.Exists(_folderPath))
                return;

            var watcher = new FileSystemWatcher(_folderPath)
            {
                Filter = "*.txt",
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Changed += (sender, e) => OnFileChanged(e.FullPath);
            watcher.Created += (sender, e) => OnFileChanged(e.FullPath);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void OnFileChanged(string fullPath)
        {
            if (_isProcessing || fullPath == _lastFile)
                return;

            _lastFile = fullPath;
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _debounceTimer.Stop();

            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                var fileToProcess = _lastFile;
                _lastFile = string.Empty;

                if (string.IsNullOrEmpty(fileToProcess))
                    return;

                using (var scope = _scopeFactory.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IFleetService>();
                    var username = TQConstants.WatcherUsername;

                    // 1. Process all files – returns only newly inserted records
                    var insertedRecords = await service.ProcessFleetFilesAsync(_folderPath, username);

                    // 2. Send notifications for each inserted record
                    foreach (var record in insertedRecords)
                    {
                        string action = record.EventType == TQConstants.EventEntry ? "entered" : "exited";
                        string message = $"<i class=\"ri-arrow-right-s-fill\"></i> Vehicle <strong>{record.VehicleNumber}</strong> {action} at {DateTime.Now:HH:mm:ss}";
                        await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, record.EventType, DateTime.Now);
                    }

                    // 3. Notify clients to refresh the dashboard
                    await _hubContext.Clients.All.SendAsync("RefreshDashboard");

                    // 4. Delete all .txt files – only if we reached this point (no exception)
                    DeleteAllTxtFiles();
                }
            }
            catch
            {
                // If any error occurs, files are NOT deleted – they remain for retry
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void DeleteAllTxtFiles()
        {
            try
            {
                var files = Directory.GetFiles(_folderPath, "*.txt");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Silently skip if a file can't be deleted (e.g., locked)
                    }
                }
            }
            catch
            {
                // Silently ignore folder errors
            }
        }
    }
}
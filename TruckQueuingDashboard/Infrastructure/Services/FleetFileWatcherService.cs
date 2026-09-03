using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FleetFileWatcherService> _logger;

        private string _lastFile = string.Empty;
        private bool _isProcessing;

        public FleetFileWatcherService(
            IServiceScopeFactory scopeFactory,
            IHubContext<FleetHub> hubContext,
            IConfiguration configuration,
            ILogger<FleetFileWatcherService> logger)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;

            _folderPath =
                configuration["Fleet:FolderPath"]
                ?? TQConstants.FleetFolderPath;

            _debounceTimer = new System.Timers.Timer(2000)
            {
                AutoReset = false
            };

            _debounceTimer.Elapsed += OnDebounceTimerElapsed;

            _logger.LogInformation(
                "FleetFileWatcherService created. Folder: {FolderPath}",
                _folderPath);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                if (!Directory.Exists(_folderPath))
                {
                    _logger.LogError(
                        "Fleet watcher folder does not exist or is inaccessible: {FolderPath}",
                        _folderPath);

                    return;
                }

                _logger.LogInformation(
                    "Fleet watcher started successfully. Watching: {FolderPath}",
                    _folderPath);

                using var watcher = new FileSystemWatcher(_folderPath)
                {
                    Filter = "*.txt",
                    EnableRaisingEvents = true,
                    NotifyFilter =
                        NotifyFilters.FileName |
                        NotifyFilters.LastWrite |
                        NotifyFilters.Size
                };

                watcher.Created += (sender, e) =>
                {
                    _logger.LogInformation(
                        "Fleet file CREATED: {FilePath}",
                        e.FullPath);

                    OnFileChanged(e.FullPath);
                };

                watcher.Changed += (sender, e) =>
                {
                    _logger.LogInformation(
                        "Fleet file CHANGED: {FilePath}",
                        e.FullPath);

                    OnFileChanged(e.FullPath);
                };

                watcher.Renamed += (sender, e) =>
                {
                    _logger.LogInformation(
                        "Fleet file RENAMED: {OldPath} -> {NewPath}",
                        e.OldFullPath,
                        e.FullPath);

                    OnFileChanged(e.FullPath);
                };

                watcher.Error += (sender, e) =>
                {
                    _logger.LogError(
                        e.GetException(),
                        "Fleet FileSystemWatcher error.");
                };

                _logger.LogInformation(
                    "FleetFileWatcherService is actively watching: {FolderPath}",
                    _folderPath);

                await Task.Delay(
                    Timeout.Infinite,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(
                    "FleetFileWatcherService stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FleetFileWatcherService failed.");
            }
        }

        private void OnFileChanged(string fullPath)
        {
            if (_isProcessing)
            {
                _logger.LogWarning(
                    "Ignoring file event because processing is already running: {FilePath}",
                    fullPath);

                return;
            }

            if (fullPath == _lastFile)
            {
                _logger.LogInformation(
                    "Ignoring duplicate file event: {FilePath}",
                    fullPath);

                return;
            }

            _lastFile = fullPath;

            _logger.LogInformation(
                "Fleet file queued for processing: {FilePath}",
                fullPath);

            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private async void OnDebounceTimerElapsed(
            object? sender,
            System.Timers.ElapsedEventArgs e)
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

                _logger.LogInformation(
                    "Processing fleet file: {FilePath}",
                    fileToProcess);

                using var scope = _scopeFactory.CreateScope();

                var service =
                    scope.ServiceProvider
                        .GetRequiredService<IFleetService>();

                var username = TQConstants.WatcherUsername;

                var insertedRecords =
                    await service.ProcessFleetFilesAsync(
                        _folderPath,
                        username);

                _logger.LogInformation(
                    "Fleet processing completed. Inserted records: {Count}",
                    insertedRecords?.Count ?? 0);

                foreach (var record in insertedRecords)
                {
                    string action =
                        record.EventType == TQConstants.EventEntry
                            ? "entered"
                            : "exited";

                    string message =
                        $"<i class=\"ri-arrow-right-s-fill\"></i> Vehicle " +
                        $"<strong>{record.VehicleNumber}</strong> {action} " +
                        $"at {DateTime.Now:HH:mm:ss}";

                    _logger.LogInformation(
                        "Broadcasting ReceiveNotification for vehicle {VehicleNumber}",
                        record.VehicleNumber);

                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveNotification",
                        message,
                        record.EventType,
                        DateTime.Now);
                }

                _logger.LogInformation(
                    "Broadcasting RefreshDashboard to all connected clients.");

                await _hubContext.Clients.All.SendAsync(
                    "RefreshDashboard");

                DeleteAllTxtFiles();

                _logger.LogInformation(
                    "Fleet file processing completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing fleet files. Files will remain for retry.");
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
                var files =
                    Directory.GetFiles(_folderPath, "*.txt");

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);

                        _logger.LogInformation(
                            "Deleted fleet file: {FilePath}",
                            file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Could not delete fleet file: {FilePath}",
                            file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while deleting fleet TXT files.");
            }
        }
    }
}
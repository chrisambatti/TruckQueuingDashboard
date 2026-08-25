using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Application.Interfaces.Repositories;
using TruckQueuingDashboard.Application.Interfaces.Services;
using TruckQueuingDashboard.Domain.Constants;
using TruckQueuingDashboard.Domain.Entities;
using TruckQueuingDashboard.Infrastructure.Hubs;
using TruckQueuingDashboard.Models;

namespace TruckQueuingDashboard.Application.Services
{
    public class FleetService : IFleetService
    {
        private readonly IFleetRepository _repository;
        private readonly IHubContext<FleetHub> _hubContext;

        public FleetService(IFleetRepository repository, IHubContext<FleetHub> hubContext)
        {
            _repository = repository;
            _hubContext = hubContext;
        }

        public async Task<List<NewFleetRecord>> ProcessFleetFilesAsync(string folderPath, string username)
        {
            return await _repository.ProcessFleetFilesAsync(folderPath, username);
        }

        public async Task<FleetDashboardDataDto> GetFleetDashboardDataAsync()
        {
            // 1. Get raw data and call queue
            var rawData = await _repository.GetFleetDashboardDataRawAsync();
            var callQueue = await _repository.GetCallQueueAsync();

            // ── 2. Get Recent Exits (latest 2 Exit events, regardless of date) ──
            // If you want exits only from today, uncomment the .Where line
            var recentExits = rawData
                .Where(e => e.Event == TQConstants.EventExit)
                // .Where(e => e.EventTimestamp.Date == DateTime.Today) // uncomment for today only
                .OrderByDescending(e => e.EventTimestamp)
                .Take(2)
                .ToList();

            // ── 3. Group by vehicle and take the latest event ──
            var grouped = rawData
                .GroupBy(e => e.FleetNumber)
                .Select(g => g
                    .OrderByDescending(e => e.EventTimestamp)
                    .ThenByDescending(e => e.Id)
                    .First())
                .ToList();

            // ── 4. Keep only trucks whose latest event is TODAY ──
            var today = DateTime.Today;
            grouped = grouped
                .Where(e => e.EventTimestamp.Date == today)
                .ToList();

            // ── 5. Mark called trucks with order from call queue ──
            foreach (var ev in grouped)
            {
                var queueItem = callQueue.FirstOrDefault(c => c.VehicleNumber == ev.FleetNumber);
                if (queueItem != null)
                {
                    ev.CalledNow = true;
                    ev.CalledNowOrder = callQueue.IndexOf(queueItem) + 1;
                }
                else
                {
                    ev.CalledNow = false;
                    ev.CalledNowOrder = 0;
                }
            }

            // ── 6. Filter Entry events ──
            var entries = grouped.Where(e => e.Event == TQConstants.EventEntry).ToList();

            // ── 7. Sort: CalledNow first by order, then oldest first ──
            var queue = entries
                .OrderBy(e => e.CalledNow ? e.CalledNowOrder : int.MaxValue)
                .ThenBy(e => e.EventTimestamp)
                .ToList();

            // ── 8. Assign turn numbers ──
            int turn = 1;
            foreach (var item in queue)
            {
                if (item.CalledNow)
                    item.Turn = item.CalledNowOrder;
                else
                    item.Turn = turn++;
            }

            // ── 9. Return result with Recent Exits ──
            return new FleetDashboardDataDto
            {
                FleetEvents = queue,
                FleetSummary = new FleetSummaryDto
                {
                    TotalEntries = queue.Count,
                    TotalExits = 0
                },
                RecentExits = recentExits
            };
        }

        public async Task<FleetDashboardDataDto> GetAllEventsAsync()
        {
            var rawData = await _repository.GetAllEventsRawAsync();
            var callQueue = await _repository.GetCallQueueAsync();

            foreach (var ev in rawData)
            {
                var queueItem = callQueue.FirstOrDefault(c => c.VehicleNumber == ev.FleetNumber);
                if (queueItem != null)
                {
                    ev.CalledNow = true;
                    ev.CalledNowOrder = callQueue.IndexOf(queueItem) + 1;
                }
                else
                {
                    ev.CalledNow = false;
                    ev.CalledNowOrder = 0;
                }
            }

            return new FleetDashboardDataDto
            {
                FleetEvents = rawData,
                FleetSummary = new FleetSummaryDto
                {
                    TotalEntries = rawData.Count(e => e.Event == TQConstants.EventEntry),
                    TotalExits = rawData.Count(e => e.Event == TQConstants.EventExit)
                }
            };
        }

        public async Task CallNowAsync(string vehicleNumber, string username)
        {
            await _repository.CallNowAsync(vehicleNumber, username);
            string message = $"<i class=\"ri-arrow-right-s-fill\"></i> Vehicle <strong>{vehicleNumber}</strong> called to the front at {System.DateTime.Now:HH:mm:ss}";
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "CallNow", System.DateTime.Now);
            await _hubContext.Clients.All.SendAsync("RefreshDashboard");
        }

        public async Task RevertCallNowAsync(string vehicleNumber)
        {
            await _repository.RevertCallNowAsync(vehicleNumber);
            string message = $"<i class=\"ri-arrow-right-s-fill\"></i> Call Now reverted for vehicle <strong>{vehicleNumber}</strong>";
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, "Revert", System.DateTime.Now);
            await _hubContext.Clients.All.SendAsync("RefreshDashboard");
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Application.Interfaces.Repositories;
using TruckQueuingDashboard.Domain.Constants;
using TruckQueuingDashboard.Domain.Entities;
using TruckQueuingDashboard.Infrastructure.Data;

namespace TruckQueuingDashboard.Infrastructure.Repositories
{
    public class FleetRepository : IFleetRepository
    {
        private readonly ApplicationDbContext _context;

        public FleetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NewFleetRecord>> ProcessFleetFilesAsync(string folderPath, string username)
        {
            if (!Directory.Exists(folderPath))
                return new List<NewFleetRecord>();

            var files = Directory.GetFiles(folderPath, "*.txt");
            if (files.Length == 0)
                return new List<NewFleetRecord>();

            var newRecords = new List<NewFleetRecord>();

            foreach (var filePath in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var parts = content.Trim().Split('|');

                    if (parts.Length < 4)
                        continue;

                    var uid = parts[0].Trim();
                    var vehicleNumber = parts[1].Trim();
                    var eventType = parts[2].Trim();
                    var timestampString = parts[3].Trim();

                    if (eventType != TQConstants.EventEntry && eventType != TQConstants.EventExit)
                        continue;

                    if (!DateTime.TryParseExact(timestampString, "dd/MM/yyyy HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var entryTimestamp))
                        continue;

                    var location = parts.Length >= 5 ? parts[4].Trim() : TQConstants.DefaultLocation;
                    if (string.IsNullOrWhiteSpace(location))
                        location = TQConstants.DefaultLocation;

                    newRecords.Add(new NewFleetRecord
                    {
                        Uid = uid,
                        VehicleNumber = vehicleNumber,
                        EventType = eventType,
                        EntryTimestamp = entryTimestamp,
                        Location = location
                    });
                }
                catch
                {
                    // skip
                }
            }

            if (newRecords.Count == 0)
                return new List<NewFleetRecord>();

            var allUids = newRecords.Select(r => r.Uid).ToList();
            var existingUids = await _context.TBatchingplants
                .Where(b => b.Uid != null && allUids.Contains(b.Uid))
                .Select(b => b.Uid)
                .ToListAsync();

            var newRecordsToInsert = newRecords
                .Where(r => !existingUids.Contains(r.Uid))
                .ToList();

            if (newRecordsToInsert.Count == 0)
                return new List<NewFleetRecord>();

            foreach (var record in newRecordsToInsert)
            {
                var newEntity = new TBatchingplant
                {
                    Uid = record.Uid,
                    VehicleNumber = record.VehicleNumber,
                    Type = record.EventType,
                    RecordTimestamp = record.EntryTimestamp,
                    Location = record.Location,
                    Status = record.EventType == "Entry" ? 1 : 0,  
                    CalledNow = false,
                    CreatedBy = username,
                    UpdatedBy = username
                };
                _context.TBatchingplants.Add(newEntity);
            }

            await _context.SaveChangesAsync();
            return newRecordsToInsert;
        }

        public async Task<List<FleetEventDto>> GetFleetDashboardDataRawAsync()
        {
            return await _context.TBatchingplants
                .Where(b => b.RecordTimestamp != null)
                .OrderByDescending(b => b.RecordTimestamp)
                .Select(b => new FleetEventDto
                {
                    Id = b.Id.ToString(),
                    FleetNumber = b.VehicleNumber ?? string.Empty,
                    Event = b.Type ?? string.Empty,
                    EventTimestamp = b.RecordTimestamp,
                    Status = b.Status ?? 0,
                    CreatedBy = b.CreatedBy ?? string.Empty,   
                    UpdatedBy = b.UpdatedBy ?? string.Empty,   
                    CalledNow = b.CalledNow ?? false,
                    CalledNowOrder = 0,
                    Location = b.Location ?? string.Empty
                })
                .ToListAsync();
        }

        public async Task<List<FleetEventDto>> GetAllEventsRawAsync()
        {
            return await _context.TBatchingplants
                .Where(b => b.RecordTimestamp != null)
                .OrderByDescending(b => b.RecordTimestamp)
                .Select(b => new FleetEventDto
                {
                    Id = b.Id.ToString(),
                    FleetNumber = b.VehicleNumber ?? string.Empty,
                    Event = b.Type ?? string.Empty,
                    EventTimestamp = b.RecordTimestamp,
                    Status = b.Status ?? 0,
                    CreatedBy = b.CreatedBy ?? string.Empty,
                    UpdatedBy = b.UpdatedBy ?? string.Empty,
                    CalledNow = b.CalledNow ?? false,
                    CalledNowOrder = 0,
                    Location = b.Location ?? string.Empty  
                })
                .ToListAsync();
        }

        public async Task<List<CallQueue>> GetCallQueueAsync()
        {
            return await _context.CallQueue.OrderBy(c => c.CalledAt).ToListAsync();
        }

        public async Task CallNowAsync(string vehicleNumber, string username)
        {
            if (string.IsNullOrWhiteSpace(vehicleNumber))
                throw new ArgumentException("Vehicle number cannot be empty.", nameof(vehicleNumber));

            var trimmed = vehicleNumber.Trim();

            var truck = await _context.TBatchingplants
                .Where(b => b.VehicleNumber.ToLower() == trimmed.ToLower() && b.Type == TQConstants.EventEntry)
                .OrderByDescending(b => b.RecordTimestamp)
                .FirstOrDefaultAsync();

            if (truck == null)
                throw new InvalidOperationException($"Vehicle '{trimmed}' not found or is not an Entry.");

            var existing = await _context.CallQueue
                .FirstOrDefaultAsync(c => c.VehicleNumber.ToLower() == trimmed.ToLower());

            if (existing != null)
                return;

            _context.CallQueue.Add(new CallQueue
            {
                VehicleNumber = trimmed,
                CalledAt = DateTime.Now
            });

            truck.CalledNow = true;
            truck.UpdatedBy = username;

            await _context.SaveChangesAsync();
        }

        public async Task RevertCallNowAsync(string vehicleNumber)
        {
            if (string.IsNullOrWhiteSpace(vehicleNumber))
                throw new ArgumentException("Vehicle number cannot be empty.", nameof(vehicleNumber));

            var trimmed = vehicleNumber.Trim();

            var call = await _context.CallQueue
                .FirstOrDefaultAsync(c => c.VehicleNumber.ToLower() == trimmed.ToLower());

            if (call != null)
                _context.CallQueue.Remove(call);

            var truck = await _context.TBatchingplants
                .Where(b => b.VehicleNumber.ToLower() == trimmed.ToLower() && b.Type == TQConstants.EventEntry)
                .OrderByDescending(b => b.RecordTimestamp)
                .FirstOrDefaultAsync();

            if (truck != null)
                truck.CalledNow = false;

            await _context.SaveChangesAsync();
        }
    }
}
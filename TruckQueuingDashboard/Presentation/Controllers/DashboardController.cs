using Microsoft.AspNetCore.Mvc;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TruckQueuingDashboard.Domain.Constants;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Application.Interfaces.Services;

namespace TruckQueuingDashboard.Presentation.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IFleetService _service;
        private readonly IConfiguration _configuration;
        private readonly string _fleetFolderPath;
        private readonly int _maxBays;

        public DashboardController(
            IFleetService service,
            IConfiguration configuration)
        {
            _service = service;
            _configuration = configuration;
            _fleetFolderPath = configuration["Fleet:FolderPath"] ?? TQConstants.FleetFolderPath;
            _maxBays = configuration.GetValue<int>("BayAllocationConfiguration:MaxNoOfAvailableBays", 2);
        }

        // ─── Main Dashboard View ────────────────────────────────────
        public async Task<IActionResult> Dispatcher()
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";
                await _service.ProcessFleetFilesAsync(_fleetFolderPath, username);

                var fleetData = await _service.GetFleetDashboardDataAsync();

                var viewModel = new DashboardViewModel
                {
                    FleetEvents = fleetData.FleetEvents ?? new List<FleetEventDto>(),
                    FleetSummary = fleetData.FleetSummary ?? new FleetSummaryDto(),
                    MaxBays = _maxBays
                };

                return View(viewModel);
            }
            catch
            {
                var viewModel = new DashboardViewModel
                {
                    FleetEvents = new List<FleetEventDto>(),
                    FleetSummary = new FleetSummaryDto(),
                    MaxBays = _maxBays
                };
                return View(viewModel);
            }
        }

        // ─── Call Now ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CallNow(string vehicleNumber)
        {
            try
            {
                var username = User?.Identity?.Name ?? "System";
                await _service.CallNowAsync(vehicleNumber, username);
                return Json(new { success = true, message = $"Truck {vehicleNumber} called to the front." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Revert Call Now ────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RevertCallNow(string vehicleNumber)
        {
            try
            {
                await _service.RevertCallNowAsync(vehicleNumber);
                return Json(new { success = true, message = $"Reverted Call Now for {vehicleNumber}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ─── Get All Events (for History Modal) ────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            try
            {
                var data = await _service.GetAllEventsAsync();
                return Json(data.FleetEvents);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ─── Get Dashboard Data (for real-time updates) ───────────
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var data = await _service.GetFleetDashboardDataAsync();
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
} 
using System.Collections.Generic;
using System.Threading.Tasks;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Domain.Entities;

namespace TruckQueuingDashboard.Application.Interfaces.Services
{
    public interface IFleetService
    {
        Task<List<NewFleetRecord>> ProcessFleetFilesAsync(string folderPath, string username);
        Task<FleetDashboardDataDto> GetFleetDashboardDataAsync();
        Task<FleetDashboardDataDto> GetAllEventsAsync();
        Task CallNowAsync(string vehicleNumber, string username);
        Task RevertCallNowAsync(string vehicleNumber);
    }
}
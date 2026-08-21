using System.Collections.Generic;
using System.Threading.Tasks;
using TruckQueuingDashboard.Application.DTOs;
using TruckQueuingDashboard.Domain.Entities;

namespace TruckQueuingDashboard.Application.Interfaces.Repositories
{
    public interface IFleetRepository
    {
        Task<List<NewFleetRecord>> ProcessFleetFilesAsync(string folderPath, string username);
        Task<List<FleetEventDto>> GetFleetDashboardDataRawAsync();
        Task<List<FleetEventDto>> GetAllEventsRawAsync();
        Task<List<CallQueue>> GetCallQueueAsync();
        Task CallNowAsync(string vehicleNumber, string username);
        Task RevertCallNowAsync(string vehicleNumber);
    }
}
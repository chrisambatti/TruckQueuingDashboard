using Microsoft.AspNetCore.SignalR;

namespace TruckQueuingDashboard.Infrastructure.Hubs
{
    public class FleetHub : Hub
    {
        public async Task RefreshDashboard()
        {
            await Clients.All.SendAsync("RefreshDashboard");
        }

        public async Task NotifyCallNow(string vehicleNumber, string username)
        {
            await Clients.All.SendAsync("TruckCalled", vehicleNumber, username);
        }

        public async Task SendNotification(string message, string type, DateTime timestamp)
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type, timestamp);
        }
    }
}
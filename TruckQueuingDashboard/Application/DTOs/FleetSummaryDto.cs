namespace TruckQueuingDashboard.Application.DTOs
{
    public class FleetSummaryDto
    {
        public int TotalEntries { get; set; }
        public int TotalExits { get; set; }
        public int TotalWaiting => TotalEntries - TotalExits; // Calculated property
    }
}
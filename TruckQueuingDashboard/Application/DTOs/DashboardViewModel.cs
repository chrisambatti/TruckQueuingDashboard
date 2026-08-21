namespace TruckQueuingDashboard.Application.DTOs
{
    public class DashboardViewModel
    {
        public List<FleetEventDto> FleetEvents { get; set; } = new();
        public FleetSummaryDto FleetSummary { get; set; } = new();
        public int MaxBays { get; set; } = 2;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
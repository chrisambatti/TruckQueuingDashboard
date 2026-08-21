namespace TruckQueuingDashboard.Application.DTOs
{
    public class FleetDashboardDataDto
    {
        public List<FleetEventDto> FleetEvents { get; set; } = new();
        public FleetSummaryDto FleetSummary { get; set; } = new();
    }
}
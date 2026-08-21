using System;

namespace TruckQueuingDashboard.Domain.Entities
{
    public class NewFleetRecord
    {
        public string Uid { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime EntryTimestamp { get; set; }
        public string Location { get; set; } = "Default Location";
    }
}
namespace TruckQueuingDashboard.Application.DTOs
{
    public class FleetEventDto
    {
        public string Id { get; set; } = string.Empty;
        public string FleetNumber { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public DateTime EventTimestamp { get; set; }
        public int Turn { get; set; }
        public int Status { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public bool CalledNow { get; set; }          
        public int CalledNowOrder { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
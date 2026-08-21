using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TruckQueuingDashboard.Domain.Entities
{
    [Table("t_call_queue")]
    public class CallQueue
    {
        [Key]
        public int Id { get; set; }

        [Column("VehicleNumber")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Column("CalledAt")]
        public DateTime CalledAt { get; set; }
    }
}
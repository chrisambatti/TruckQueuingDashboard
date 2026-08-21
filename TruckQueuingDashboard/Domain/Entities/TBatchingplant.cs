using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TruckQueuingDashboard.Domain.Entities;

public partial class TBatchingplant
{
    public int Id { get; set; }

    public string? Uid { get; set; }

    public string VehicleNumber { get; set; } = null!;

    public string? Location { get; set; }

    public string Type { get; set; } = null!;

    public DateTime RecordTimestamp { get; set; }

    public bool? CalledNow { get; set; }

    public int? Status { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }
}

using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class FacilityStat
{
    public int FacilityStatId { get; set; }

    public int FacilityId { get; set; }

    public string StatNumber { get; set; } = null!;

    public string StatLabel { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Facility Facility { get; set; } = null!;
}

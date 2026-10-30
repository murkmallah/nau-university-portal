using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class FacilityService
{
    public int FacilityServiceId { get; set; }

    public int FacilityId { get; set; }

    public string? Icon { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public virtual Facility Facility { get; set; } = null!;
}

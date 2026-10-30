using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class FacilityCardTag
{
    public int FacilityCardTagId { get; set; }

    public int FacilityId { get; set; }

    public string? Icon { get; set; }

    public string Label { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Facility Facility { get; set; } = null!;
}
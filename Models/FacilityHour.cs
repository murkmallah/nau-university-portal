using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class FacilityHour
{
    public int FacilityHourId { get; set; }

    public int FacilityId { get; set; }

    public string DayOrLabel { get; set; } = null!;

    public string TimeValue { get; set; } = null!;

    public bool IsHighlighted { get; set; }

    public int DisplayOrder { get; set; }

    public virtual Facility Facility { get; set; } = null!;
}

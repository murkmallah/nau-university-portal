using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class Facility
{
    public int FacilityId { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? HeroImageUrl { get; set; }

    public string? IntroText { get; set; }

    public string? ColorHex { get; set; }

    // ---- Naye fields: Facilities grid/listing card ke liye ----

    public string? Category { get; set; }

    public string? CardIcon { get; set; }

    public string? GradientColorHex2 { get; set; }

    public string? ShortHours { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    // -------------------------------------------------------------

    public virtual ICollection<FacilityHour> FacilityHours { get; set; } = new List<FacilityHour>();

    public virtual ICollection<FacilityService> FacilityServices { get; set; } = new List<FacilityService>();

    public virtual ICollection<FacilityStat> FacilityStats { get; set; } = new List<FacilityStat>();

    public virtual ICollection<FacilityCardTag> FacilityCardTags { get; set; } = new List<FacilityCardTag>();
}
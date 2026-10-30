using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class SimulationLabSection
{
    public int SimulationLabSectionId { get; set; }

    // e.g. "flight-simulators", "atc-simulator", "avionics-lab"
    public string SectionKey { get; set; } = null!;

    public string? Tag { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? IconClass { get; set; }

    public string? ColorHex { get; set; }

    public string? ColorHex2 { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int DisplayOrder { get; set; }

    public virtual ICollection<SimulationLabImage> SimulationLabImages { get; set; } = new List<SimulationLabImage>();

    public virtual ICollection<SimulationLabFeature> SimulationLabFeatures { get; set; } = new List<SimulationLabFeature>();

    public virtual SimulationLabInfoCard? SimulationLabInfoCard { get; set; }
}

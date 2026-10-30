using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class SimulationLabFeature
{
    public int SimulationLabFeatureId { get; set; }

    public int SimulationLabSectionId { get; set; }

    public string? IconClass { get; set; }

    public string Label { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual SimulationLabSection SimulationLabSection { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class SimulationLabInfoItem
{
    public int SimulationLabInfoItemId { get; set; }

    public int SimulationLabInfoCardId { get; set; }

    public string Text { get; set; } = null!;

    // dot color next to the bullet, e.g. "var(--atc-a)"
    public string? ColorHex { get; set; }

    public int DisplayOrder { get; set; }

    public virtual SimulationLabInfoCard SimulationLabInfoCard { get; set; } = null!;
}

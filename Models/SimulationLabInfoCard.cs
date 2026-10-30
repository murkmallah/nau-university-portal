using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class SimulationLabInfoCard
{
    public int SimulationLabInfoCardId { get; set; }

    public int SimulationLabSectionId { get; set; }

    // e.g. "Simulator Capabilities", "Lab Equipment"
    public string Heading { get; set; } = null!;

    // Optional bottom highlight box (e.g. "ICAO Doc 9868 Compliant Training").
    // Null/empty = highlight box not rendered.
    public string? HighlightText { get; set; }

    public string? HighlightIcon { get; set; }

    // e.g. "purple", "amber" — matches existing highlight-box color classes
    public string? HighlightColor { get; set; }

    public virtual SimulationLabSection SimulationLabSection { get; set; } = null!;

    public virtual ICollection<SimulationLabInfoItem> SimulationLabInfoItems { get; set; } = new List<SimulationLabInfoItem>();
}

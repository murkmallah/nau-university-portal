using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class SimulationLabImage
{
    public int SimulationLabImageId { get; set; }

    public int SimulationLabSectionId { get; set; }

    // Real uploaded photo (via /Cms/UploadImage). Null/empty falls back to
    // the icon + gradient placeholder tile, same pattern as GalleryPhoto.
    public string? ImageUrl { get; set; }

    public string? IconClass { get; set; }

    public string? Title { get; set; }

    public string? Badge { get; set; }

    // true = the big tile on the left of the grid, false = one of the two small tiles
    public bool IsLarge { get; set; }

    public int DisplayOrder { get; set; }

    public virtual SimulationLabSection SimulationLabSection { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

// One tile in the "Photo Gallery" section of the News & Media public page.
// Admin-managed only -- created/edited/deleted from the live page while in
// Edit Mode, same mechanism as Courses/NewsPosts (QuickAdd/QuickUpdate/QuickDelete).
public partial class GalleryPhoto
{
    public int PhotoId { get; set; }

    // Short line shown under the photo (e.g. "Annual Convocation 2025 - Degree Award Ceremony")
    public string Caption { get; set; } = null!;

    // Album/category label used for the filter buttons (e.g. "CPD & Seminars", "Training & Labs",
    // "Campus Life", "Ceremonies"). Free text -- the filter bar is built from whatever
    // distinct albums currently exist, same pattern as the News category filter.
    public string Album { get; set; } = null!;

    // Real uploaded photo (via /Cms/UploadImage). Null/empty falls back to a
    // gradient + icon placeholder tile so the grid never looks broken.
    public string? ImageUrl { get; set; }

    // Font Awesome icon class used only when ImageUrl is empty.
    public string? Icon { get; set; }

    public int DisplayOrder { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ApplicationUser? CreatedByUser { get; set; }
}

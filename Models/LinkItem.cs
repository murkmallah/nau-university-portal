using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

// One row on the "Useful Links" page. Admin-managed only -- created /
// edited / deleted from the live page while in Edit Mode, same mechanism
// as Courses/NewsPosts/GalleryPhotos/GalleryVideos (QuickAdd/QuickUpdate/QuickDelete).
public partial class LinkItem
{
    public int LinkId { get; set; }

    // Link title shown on the card (e.g. "LMS", "E-library")
    public string Name { get; set; } = null!;

    // Short description shown under the title
    public string Description { get; set; } = null!;

    // Destination URL the card links to
    public string Url { get; set; } = null!;

    // Font Awesome icon class shown on the card (e.g. "fas fa-link")
    public string Icon { get; set; } = "fas fa-link";

    public int DisplayOrder { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ApplicationUser? CreatedByUser { get; set; }
}

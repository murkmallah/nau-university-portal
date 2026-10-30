using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

// One tile in the "Video Gallery" section of the News & Media public page.
// Admin-managed only -- created/edited/deleted from the live page while in
// Edit Mode, same mechanism as Courses/NewsPosts/GalleryPhotos (QuickAdd/QuickUpdate/QuickDelete).
public partial class GalleryVideo
{
    public int VideoId { get; set; }

    // Headline shown under the thumbnail (e.g. "NAU Campus Tour & Overview 2026")
    public string Title { get; set; } = null!;

    // Category/album label used for the colored badge (e.g. "Campus Tour", "Training",
    // "CPD", "Ceremony", "Campus Life"). Free text -- filter/coloring is derived from it.
    public string Category { get; set; } = null!;

    // Where the video actually plays from. Accepts a YouTube URL (youtube.com/watch?v=...
    // or youtu.be/...) or a direct file link (e.g. an uploaded .mp4 from /Cms/UploadImage
    // or wwwroot/uploads). The public page auto-detects which one it is.
    public string VideoUrl { get; set; } = null!;

    // Optional cover image for the thumbnail. Null/empty falls back to a
    // gradient + icon placeholder tile so the grid never looks broken.
    public string? ThumbnailUrl { get; set; }

    // Font Awesome icon class used only when ThumbnailUrl is empty.
    public string? Icon { get; set; }

    // Small duration badge shown on the thumbnail, e.g. "4:38". Optional/free text.
    public string? Duration { get; set; }

    public int Views { get; set; }

    public DateOnly PublishDate { get; set; }

    public int DisplayOrder { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ApplicationUser? CreatedByUser { get; set; }
}

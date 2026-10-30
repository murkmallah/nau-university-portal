using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class NewsPost
{
    public int NewsPostId { get; set; }

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string? Category { get; set; }

    public string? Icon { get; set; }

    public string? ImageUrl { get; set; }

    public DateOnly PublishDate { get; set; }

    public DateOnly? ExpiresAt { get; set; }

    public bool IsFeatured { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSplashScreen { get; set; } = false;

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ApplicationUser? CreatedByUser { get; set; }
}

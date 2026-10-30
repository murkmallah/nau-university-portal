using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class Page
{
    public int PageId { get; set; }

    public string Slug { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? HeroImageUrl { get; set; }

    public string? ContentHtml { get; set; }

    public bool IsPublished { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public virtual ApplicationUser? UpdatedByUser { get; set; }
}

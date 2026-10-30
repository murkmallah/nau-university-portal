using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class Event
{
    public int EventId { get; set; }

    public string Title { get; set; } = null!;

    public string? Location { get; set; }

    public DateOnly EventDate { get; set; }

    public DateOnly? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int? CreatedByUserId { get; set; }

    public virtual ApplicationUser? CreatedByUser { get; set; }
}

using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class VwCourseStatus
{
    public int CourseId { get; set; }

    public int SchoolId { get; set; }

    public string Title { get; set; } = null!;

    public string? Duration { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsTbd { get; set; }

    public string Status { get; set; } = null!;
}

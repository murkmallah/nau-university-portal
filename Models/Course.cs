using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public int SchoolId { get; set; }

    public string Title { get; set; } = null!;

    public string? Duration { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsTbd { get; set; }

    public string? Description { get; set; }

    public string? Topics { get; set; }

    public string? Highlights { get; set; }

    public string? Certificate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<AcademicCalendarResult> AcademicCalendarResults { get; set; } = new List<AcademicCalendarResult>();

    public virtual School School { get; set; } = null!;
}

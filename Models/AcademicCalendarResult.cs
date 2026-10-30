using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class AcademicCalendarResult
{
    public int ResultId { get; set; }

    public int? CourseId { get; set; }

    public string Label { get; set; } = null!;

    public DateOnly ResultDate { get; set; }

    public string ResultType { get; set; } = null!;

    public virtual Course? Course { get; set; }
}

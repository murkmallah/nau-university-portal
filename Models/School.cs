using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class School
{
    public int SchoolId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public string? Icon { get; set; }

    public string? ColorHex { get; set; }

    public string? Slug { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}

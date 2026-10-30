using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class Faculty
{
    public int FacultyId { get; set; }

    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Icon { get; set; }

    public string? ColorHex { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Program> Programs { get; set; } = new List<Program>();
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NAU_Web.Models;

public partial class Program
{
    public int ProgramId { get; set; }

    public int FacultyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Department { get; set; }

    public string? Icon { get; set; }

    public string? ColorHex { get; set; }

    public string? Overview { get; set; }

    public string? UndergradYears { get; set; }

    public string? Adpyears { get; set; }

    public string? GradYears { get; set; }

    public virtual Faculty Faculty { get; set; } = null!;

    public virtual ICollection<ProgramCareer> ProgramCareers { get; set; } = new List<ProgramCareer>();

    public virtual ICollection<ProgramHighlight> ProgramHighlights { get; set; } = new List<ProgramHighlight>();
}

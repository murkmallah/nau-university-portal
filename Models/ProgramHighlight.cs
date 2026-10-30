using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class ProgramHighlight
{
    public int ProgramHighlightId { get; set; }

    public int ProgramId { get; set; }

    public string HighlightText { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public virtual Program Program { get; set; } = null!;
}

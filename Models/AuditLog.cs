using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class AuditLog
{
    public int AuditLogId { get; set; }

    public int UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? Details { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

public partial class MediaLibrary
{
    public int MediaId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? FileType { get; set; }

    public long? FileSizeBytes { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual ApplicationUser? UploadedByUser { get; set; }
}

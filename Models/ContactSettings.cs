using System;
using System.Collections.Generic;

namespace NAU_Web.Models;

// Singleton settings row -- there is always exactly 1 row in this table.
public partial class ContactSettings
{
    public int ContactSettingsId { get; set; }

    public string Heading { get; set; } = null!;

    public string? Subtext { get; set; }

    public string? Phone { get; set; }

    // Also used as the recipient address for Contact form submissions
    // (ContactController.Send), replacing the old Smtp:ToEmail-only setup.
    public string Email { get; set; } = null!;

    public string? Address { get; set; }

    public string? OfficeHours { get; set; }

    // Gmail address used as the SMTP login + "From" address when sending
    // Contact form emails. Falls back to appsettings.json's Smtp:Username
    // when null/empty (see SmtpEmailSender).
    public string? SmtpUsername { get; set; }

    // Gmail App Password for SmtpUsername. Stored as plain text (Admin-only
    // access, same as the rest of this settings row) -- falls back to
    // appsettings.json's Smtp:Password when null/empty.
    public string? SmtpPassword { get; set; }
}

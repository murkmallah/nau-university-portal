using System.Threading.Tasks;

namespace NAU_Web.Services
{
    public interface IEmailSender
    {
        // toEmailOverride: when provided, sends here instead of appsettings.json's
        // Smtp:ToEmail (used by ContactController to route to the DB-configured
        // ContactSettings.Email so admins can change the recipient from the UI).
        // smtpUsernameOverride/smtpPasswordOverride: when provided, used as the
        // SMTP login + "From" address instead of appsettings.json's Smtp:Username
        // / Smtp:Password (used by ContactController to route to the DB-configured
        // ContactSettings.SmtpUsername/SmtpPassword so admins can change the
        // sending Gmail account from the UI).
        Task SendEmailAsync(string subject, string body, string? replyToEmail = null, string? replyToName = null, string? toEmailOverride = null, string? smtpUsernameOverride = null, string? smtpPasswordOverride = null);
    }
}

using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NAU_Web.Services
{
    // Sends the Contact Us form as an email using the SMTP settings under the
    // "Smtp" section of appsettings.json. No extra NuGet package needed --
    // this uses the built-in System.Net.Mail client.
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string subject, string body, string? replyToEmail = null, string? replyToName = null, string? toEmailOverride = null, string? smtpUsernameOverride = null, string? smtpPasswordOverride = null)
        {
            var host = _config["Smtp:Host"];
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) || ssl; // default true
            var username = string.IsNullOrWhiteSpace(smtpUsernameOverride) ? _config["Smtp:Username"] : smtpUsernameOverride;
            var password = string.IsNullOrWhiteSpace(smtpPasswordOverride) ? _config["Smtp:Password"] : smtpPasswordOverride;
            var fromEmail = string.IsNullOrWhiteSpace(smtpUsernameOverride) ? (_config["Smtp:FromEmail"] ?? username) : smtpUsernameOverride;
            var fromName = _config["Smtp:FromName"] ?? "NAU Website";
            var toEmail = toEmailOverride ?? _config["Smtp:ToEmail"] ?? fromEmail;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogError("Smtp settings are missing in appsettings.json (Host/FromEmail/ToEmail). Contact form email was not sent.");
                throw new InvalidOperationException("Email is not configured. Please set the Smtp section in appsettings.json.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                message.ReplyToList.Add(new MailAddress(replyToEmail, replyToName ?? replyToEmail));
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrWhiteSpace(username) ? null : new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}

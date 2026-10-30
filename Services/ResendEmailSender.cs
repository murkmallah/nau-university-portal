using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NAU_Web.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(HttpClient httpClient, IConfiguration config, ILogger<ResendEmailSender> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        private class ResendEmailRequest
        {
            [JsonPropertyName("from")]
            public string From { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = Array.Empty<string>();

            [JsonPropertyName("subject")]
            public string Subject { get; set; } = string.Empty;

            [JsonPropertyName("html")]
            public string Html { get; set; } = string.Empty;

            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("reply_to")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ReplyTo { get; set; }
        }

        public async Task SendEmailAsync(
            string subject,
            string body,
            string? replyToEmail = null,
            string? replyToName = null,
            string? toEmailOverride = null,
            string? smtpUsernameOverride = null,
            string? smtpPasswordOverride = null)
        {
            var apiKey = _config["Resend:ApiKey"] ;
            var fromEmail = _config["Resend:FromEmail"] ?? "NAU Website <onboarding@resend.dev>";
            var fallbackToEmail = _config["Resend:ToEmail"];
            var primaryToEmail = !string.IsNullOrWhiteSpace(toEmailOverride) ? toEmailOverride.Trim() : fallbackToEmail;

            var smtpUsername = !string.IsNullOrWhiteSpace(smtpUsernameOverride) ? smtpUsernameOverride.Trim() : _config["Smtp:Username"];
            var smtpPassword = !string.IsNullOrWhiteSpace(smtpPasswordOverride) ? smtpPasswordOverride.Trim() : _config["Smtp:Password"];
            var smtpHost = _config["Smtp:Host"] ?? "smtp.gmail.com";

            // 1. If SMTP credentials are provided, attempt SMTP delivery first
            if (!string.IsNullOrWhiteSpace(smtpUsername) && !string.IsNullOrWhiteSpace(smtpPassword) && !smtpUsername.Contains("your-sending-address"))
            {
                _logger.LogInformation("Attempting SMTP delivery to {ToEmail} using account {SmtpUser}...", primaryToEmail, smtpUsername);
                try
                {
                    await SendViaSmtpAsync(smtpHost, smtpUsername, smtpPassword, primaryToEmail, subject, body, replyToEmail, replyToName);
                    _logger.LogInformation("Email successfully sent via SMTP to {ToEmail}", primaryToEmail);
                    return;
                }
                catch (Exception smtpEx)
                {
                    _logger.LogWarning(smtpEx, "SMTP delivery to {ToEmail} failed ({Message}). Trying Resend API...", primaryToEmail, smtpEx.Message);
                }
            }

            // 2. Try sending directly to the admin-configured destination email via Resend
            var formattedHtml = BuildHtmlEmail(subject, body, replyToEmail, replyToName, primaryToEmail);
            bool sent = await TrySendViaResendAsync(apiKey, fromEmail, primaryToEmail, subject, body, formattedHtml, replyToEmail);

            if (sent)
            {
                _logger.LogInformation("Email successfully sent via Resend directly to dynamic recipient {ToEmail}", primaryToEmail);
                return;
            }

            // 3. Resend fallback: Deliver to account owner with clear indication of target destination
            if (!string.Equals(primaryToEmail, fallbackToEmail, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Delivering to Resend account owner {FallbackEmail} with dynamic target {TargetEmail} notice.", fallbackToEmail, primaryToEmail);
                var fallbackSubject = $"[For: {primaryToEmail}] {subject}";
                var fallbackNotice = $"[Destination: {primaryToEmail}]\n\n" + body;
                var fallbackHtml = BuildHtmlEmail(fallbackSubject, body, replyToEmail, replyToName, primaryToEmail, isFallbackNotice: true, fallbackAccountEmail: fallbackToEmail);

                bool fallbackSent = await TrySendViaResendAsync(apiKey, fromEmail, fallbackToEmail, fallbackSubject, fallbackNotice, fallbackHtml, replyToEmail);
                if (!fallbackSent)
                {
                    throw new HttpRequestException($"Failed to send email via Resend to {primaryToEmail} and fallback {fallbackToEmail}.");
                }
            }
        }

        private static string BuildHtmlEmail(string subject, string body, string? replyToEmail, string? replyToName, string targetEmail, bool isFallbackNotice = false, string? fallbackAccountEmail = null)
        {
            var fallbackBanner = isFallbackNotice ? $@"
    <div style=""background:#fffbeb; border:1px solid #fef3c7; border-left:4px solid #f59e0b; padding:14px 18px; border-radius:6px; margin-bottom:18px; font-size:13px; color:#92400e;"">
      <strong>⚠️ Resend Testing Mode Notice:</strong><br/>
      This message was sent to <strong>{System.Net.WebUtility.HtmlEncode(targetEmail)}</strong>.<br/>
      Because this Resend API key is in testing mode (using <code>onboarding@resend.dev</code>), Resend routed it to the account email (<strong>{System.Net.WebUtility.HtmlEncode(fallbackAccountEmail ?? "")}</strong>).
    </div>" : "";

            // If the body is already complete structured HTML, deliver it directly (with fallback notice prepended if applicable)
            if (body.TrimStart().StartsWith("<div", StringComparison.OrdinalIgnoreCase) ||
                body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                body.TrimStart().StartsWith("<table", StringComparison.OrdinalIgnoreCase))
            {
                return isFallbackNotice ? (fallbackBanner + body) : body;
            }

            return $@"<div style=""font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height:1.6; color:#2d3748; max-width:600px; margin:0 auto; border:1px solid #e2e8f0; border-radius:8px; overflow:hidden;"">
  <div style=""background:linear-gradient(135deg, #0b2c4d 0%, #0d3a6e 100%); padding:24px 28px; color:#ffffff;"">
    <h2 style=""margin:0; font-size:20px; font-weight:700; color:#ffffff;"">National Aviation University</h2>
    <p style=""margin:4px 0 0 0; font-size:13px; color:#a0aec0; letter-spacing:1.5px; text-transform:uppercase;"">Notification</p>
  </div>
  <div style=""padding:28px;"">
    {fallbackBanner}
    <div style=""background:#f7fafc; border-left:4px solid #1e8e3e; padding:18px 20px; border-radius:4px; margin-bottom:20px;"">
      <div style=""font-size:14px; white-space:pre-wrap; color:#2d3748;"">{System.Net.WebUtility.HtmlEncode(body)}</div>
    </div>
    <div style=""font-size:12px; color:#718096; border-top:1px solid #edf2f7; padding-top:14px;"">
      <p style=""margin:0;""><strong>Target Recipient:</strong> {System.Net.WebUtility.HtmlEncode(targetEmail)}</p>
      <p style=""margin:4px 0 0 0;""><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(subject)}</p>
      {(string.IsNullOrWhiteSpace(replyToEmail) ? "" : $"<p style=\"margin:4px 0 0 0;\"><strong>Reply-To:</strong> {System.Net.WebUtility.HtmlEncode(replyToName ?? replyToEmail)} &lt;{System.Net.WebUtility.HtmlEncode(replyToEmail)}&gt;</p>")}
    </div>
  </div>
  <div style=""background:#edf2f7; padding:12px 28px; text-align:center; font-size:11px; color:#a0aec0;"">
    This email was sent automatically from National Aviation University (NAU) Portal.
  </div>
</div>";
        }

        private async Task<bool> TrySendViaResendAsync(
            string apiKey,
            string fromEmail,
            string toEmail,
            string subject,
            string textBody,
            string htmlBody,
            string? replyToEmail)
        {
            try
            {
                var payload = new ResendEmailRequest
                {
                    From = fromEmail,
                    To = new[] { toEmail },
                    Subject = subject,
                    Text = textBody,
                    Html = htmlBody,
                    ReplyTo = string.IsNullOrWhiteSpace(replyToEmail) ? null : replyToEmail
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Contact email sent successfully via Resend to {ToEmail}. Response: {Response}", toEmail, responseContent);
                    return true;
                }

                _logger.LogWarning("Resend API returned {StatusCode} for {ToEmail}: {Response}", response.StatusCode, toEmail, responseContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending email via Resend to {ToEmail}", toEmail);
                return false;
            }
        }

        private async Task SendViaSmtpAsync(
            string host,
            string username,
            string password,
            string toEmail,
            string subject,
            string body,
            string? replyToEmail,
            string? replyToName)
        {
            var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
            var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) || ssl;

            var isHtml = body.TrimStart().StartsWith("<div", StringComparison.OrdinalIgnoreCase) ||
                         body.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                         body.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                         body.TrimStart().StartsWith("<table", StringComparison.OrdinalIgnoreCase);

            using var message = new MailMessage
            {
                From = new MailAddress(username, "National Aviation University"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            message.To.Add(toEmail);

            if (!string.IsNullOrWhiteSpace(replyToEmail))
            {
                message.ReplyToList.Add(new MailAddress(replyToEmail, replyToName ?? replyToEmail));
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}

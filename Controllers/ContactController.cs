using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    public class ContactController : Controller
    {
        private readonly NauDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ContactController> _logger;
        private readonly IUserPermissionService _permissionService;

        public ContactController(
            NauDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            ILogger<ContactController> logger,
            IUserPermissionService permissionService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _permissionService = permissionService;
        }

        // Same pattern used on every other public page (SimulationLabs, Facilities, ...)
        // so the shared header shows "Hi, <name>" / Admin Panel correctly for a logged-in user.
        private async Task SetCurrentUserNameAsync()
        {
            ViewBag.CurrentUserName = "";
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.CurrentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity.Name ?? "");
            }
        }

        // Loads the single ContactSettings row, creating it on first run with the
        // same content the page used to show as static HTML (heading, subtext,
        // phone, email, address, office hours) -- same pattern as the
        // Ensure*InfoCardAsync seeding in SimulationLabsController -- so the page
        // never appears empty and nothing changes visually until an admin edits it.
        private async Task<ContactSettings> GetOrCreateContactSettingsAsync()
        {
            var settings = await _context.ContactSettings.FirstOrDefaultAsync();
            if (settings != null) return settings;

            settings = new ContactSettings
            {
                Heading = "Contact Us",
                Subtext = "Have a question about admissions, courses or campus life? Reach out and our team will get back to you shortly.",
                Phone = "+92 22 9260310-19",
                Email = "AdID.Academics@paa.gov.pk",
                Address = "Airport Road, Hyderabad, Sindh, Pakistan",
                OfficeHours = "Monday to Saturday, 09:00 AM to 06:00 PM"
            };
            _context.ContactSettings.Add(settings);
            await _context.SaveChangesAsync();

            return settings;
        }

        // GET /Contact
        [HttpGet]
        public async Task<IActionResult> Index(string? edit = null)
        {
            await SetCurrentUserNameAsync();

            bool wantsEdit = edit == "true" || edit == "1";
            ApplicationUser? currentUser = null;
            bool canEdit = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    canEdit = await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentContact)
                           || await _permissionService.IsSuperAdminAsync(currentUser.Id);
                }
            }

            ViewBag.IsAdmin = canEdit;
            ViewBag.EditMode = wantsEdit && canEdit;

            var settings = await GetOrCreateContactSettingsAsync();

            return View(settings);
        }

        public class QuickSaveContactSettingsRequest
        {
            public string? Heading { get; set; }
            public string? Subtext { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Address { get; set; }
            public string? OfficeHours { get; set; }
            public string? SmtpUsername { get; set; }
            public string? SmtpPassword { get; set; }
        }

        // POST: /Contact/QuickSaveContactSettings
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSaveContactSettings([FromBody] QuickSaveContactSettingsRequest req)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (!await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentContact) && !await _permissionService.IsSuperAdminAsync(currentUser.Id)))
            {
                return Forbid();
            }
            if (req == null || string.IsNullOrWhiteSpace(req.Heading) || string.IsNullOrWhiteSpace(req.Email))
            {
                return BadRequest(new { success = false, message = "Heading and Email are required." });
            }

            var settings = await GetOrCreateContactSettingsAsync();

            settings.Heading = req.Heading.Trim();
            settings.Subtext = string.IsNullOrWhiteSpace(req.Subtext) ? null : req.Subtext.Trim();
            settings.Phone = string.IsNullOrWhiteSpace(req.Phone) ? null : req.Phone.Trim();
            settings.Email = req.Email.Trim();
            settings.Address = string.IsNullOrWhiteSpace(req.Address) ? null : req.Address.Trim();
            settings.OfficeHours = string.IsNullOrWhiteSpace(req.OfficeHours) ? null : req.OfficeHours.Trim();
            settings.SmtpUsername = string.IsNullOrWhiteSpace(req.SmtpUsername) ? null : req.SmtpUsername.Trim();
            settings.SmtpPassword = string.IsNullOrWhiteSpace(req.SmtpPassword) ? null : req.SmtpPassword.Trim();

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST /Contact/Send  (called via fetch() from Views/Contact/Index.cshtml)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send([FromBody] ContactMessageViewModel model)
        {
            if (model == null)
            {
                return Json(new { success = false, message = "Invalid submission." });
            }

            // Honeypot: real visitors never fill this hidden field.
            if (!string.IsNullOrWhiteSpace(model.Website))
            {
                return Json(new { success = true }); // pretend success, drop silently
            }

            if (!ModelState.IsValid)
            {
                var firstError = "Please check the form and try again.";
                foreach (var state in ModelState.Values)
                {
                    foreach (var error in state.Errors)
                    {
                        firstError = error.ErrorMessage;
                        break;
                    }
                    if (firstError != "Please check the form and try again.") break;
                }
                return Json(new { success = false, message = firstError });
            }

            var fullName = string.IsNullOrWhiteSpace(model.LastName)
                ? model.FirstName
                : $"{model.FirstName} {model.LastName}";

            var body = new StringBuilder();
            body.AppendLine("New message from the NAU website Contact Us form.");
            body.AppendLine();
            body.AppendLine($"Name: {fullName}");
            body.AppendLine($"Mobile No: {model.MobileNo}");
            body.AppendLine($"Email: {model.Email}");
            body.AppendLine();
            body.AppendLine("Message:");
            body.AppendLine(string.IsNullOrWhiteSpace(model.Message) ? "(no message provided)" : model.Message);

            // Recipient now comes from the DB (admin-editable via QuickSaveContactSettings).
            // appsettings.json's Smtp:ToEmail is only used as a fallback if the DB
            // row somehow has no email set.
            var settings = await _context.ContactSettings.FirstOrDefaultAsync();
            var toEmail = string.IsNullOrWhiteSpace(settings?.Email) ? null : settings.Email;

            // Sending Gmail account also comes from the DB now (admin-editable via
            // QuickSaveContactSettings). appsettings.json's Smtp:Username/Password
            // are only used as a fallback if the DB row has no SMTP credentials set.
            var smtpUsername = string.IsNullOrWhiteSpace(settings?.SmtpUsername) ? null : settings.SmtpUsername;
            var smtpPassword = string.IsNullOrWhiteSpace(settings?.SmtpPassword) ? null : settings.SmtpPassword;

            try
            {
                await _emailSender.SendEmailAsync(
                    subject: $"NAU Contact Form: {fullName}",
                    body: body.ToString(),
                    replyToEmail: model.Email,
                    replyToName: fullName,
                    toEmailOverride: toEmail,
                    smtpUsernameOverride: smtpUsername,
                    smtpPasswordOverride: smtpPassword);

                return Json(new { success = true, message = "Thank you! Your message has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact form email.");
                return Json(new { success = false, message = "Sorry, something went wrong sending your message. Please try again later or call us directly." });
            }
        }

        // POST /Contact/SendTestEmail
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail()
        {
            var settings = await GetOrCreateContactSettingsAsync();
            var toEmail = string.IsNullOrWhiteSpace(settings?.Email) ? "AdID.Academics@paa.gov.pk" : settings.Email.Trim();
            var smtpUsername = string.IsNullOrWhiteSpace(settings?.SmtpUsername) ? null : settings.SmtpUsername.Trim();
            var smtpPassword = string.IsNullOrWhiteSpace(settings?.SmtpPassword) ? null : settings.SmtpPassword.Trim();

            var body = new StringBuilder();
            body.AppendLine("Hello!");
            body.AppendLine();
            body.AppendLine("This is a verified test email sent from the National Aviation University Admin Panel.");
            body.AppendLine($"All website inquiries and Contact Us form submissions are now configured to be received at this address: {toEmail}");
            body.AppendLine();
            body.AppendLine($"Timestamp: {DateTime.Now:dddd, dd MMMM yyyy, hh:mm:ss tt}");

            try
            {
                await _emailSender.SendEmailAsync(
                    subject: "NAU Website: Test Email Verification",
                    body: body.ToString(),
                    replyToEmail: null,
                    replyToName: "NAU Administration",
                    toEmailOverride: toEmail,
                    smtpUsernameOverride: smtpUsername,
                    smtpPasswordOverride: smtpPassword);

                return Json(new { success = true, message = $"✅ Test email successfully sent to {toEmail}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {ToEmail}", toEmail);
                return Json(new { success = false, message = $"Failed to send test email: {ex.Message}" });
            }
        }
    }
}

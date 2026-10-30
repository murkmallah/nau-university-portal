using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Kestrel's default max request body size (~28.6 MB) is lower than the
// video upload limit set in CmsController, so large hero-video uploads
// (e.g. a full-quality 46 MB clip) were being rejected by the server
// before ever reaching the controller -- silently failing the upload,
// or leaving an older/smaller test video as the only one that ever
// made it through. Raise it here to match the app-level 300 MB cap.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null; // Allow large video uploads via Kestrel
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 300 * 1024 * 1024; // 300 MB
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.Services.AddDbContext<NauDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("NAU_CMS")));

// Persist Data Protection keys to disk so login cookies stay valid across app
// restarts (without this, every restart invalidates existing login cookies
// and forces everyone to log in again).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys")))
    .SetApplicationName("NAU_Web");

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.SignIn.RequireConfirmedAccount = true;
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddEntityFrameworkStores<NauDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    // Stay logged in until the user explicitly logs out.
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// The Short Courses admin edit page (Views/Courses/Public.cshtml) sends the
// antiforgery token as a "RequestVerificationToken" HTTP header on its
// fetch() calls (Add/Edit/Delete course & school). By default ASP.NET Core
// only validates the token from a form field, so those JSON-body requests
// were always rejected -- which is why the delete/add buttons silently
// failed. Telling it to also accept the header fixes that.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Sends the Contact Us form (Views/Contact/Index.cshtml) as an email using
// the Resend API (configured under "Resend" in appsettings.json).
builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailSender, ResendEmailSender>();

// Real-time Database & CMS connected AI Chatbot services
builder.Services.AddScoped<IChatbotKnowledgeService, ChatbotKnowledgeService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// Dynamic Navigation Menu Management Service
builder.Services.AddScoped<INavMenuService, NavMenuService>();

// Dynamic Site-Wide Announcement Bar Service
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();

// Dynamic Site-Wide Branding & Logo Service
builder.Services.AddScoped<ISiteBrandingService, SiteBrandingService>();

// Dynamic Site-Wide Footer Service
builder.Services.AddScoped<IFooterService, FooterService>();

// Dynamic Schools Management Service
builder.Services.AddScoped<ISchoolService, SchoolService>();

// Real-Time Site Uptime & Downtime Health Monitor Service
builder.Services.AddSingleton<SiteUptimeMonitorService>();
builder.Services.AddSingleton<ISiteUptimeMonitorService>(sp => sp.GetRequiredService<SiteUptimeMonitorService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<SiteUptimeMonitorService>());

// Real-Time Visitor Analytics & Country Telemetry Service
builder.Services.AddSingleton<IVisitorAnalyticsService, VisitorAnalyticsService>();

// Granular Role-Based Access Control (RBAC) & User Permission Service
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

// Production Health Checks for external uptime services
builder.Services.AddHealthChecks();

var app = builder.Build();

// One-time seed: the public News page (Views/NewsPost/Public.cshtml) now reads
// from the NewsPosts table instead of static HTML. On a fresh/empty table,
// load in the original sample news items from the reference design so the
// page isn't blank the first time it's opened. This only runs when the table
// has zero rows, so it never overwrites or duplicates anything an admin adds,
// edits, or deletes afterwards.
using (var seedScope = app.Services.CreateScope())
{
    var seedDb = seedScope.ServiceProvider.GetRequiredService<NauDbContext>();
    if (!seedDb.NewsPosts.Any())
    {
        seedDb.NewsPosts.AddRange(
            new NewsPost
            {
                Title = "14th ESP — Experience Sharing Platform Successfully Conducted",
                Body = "The 14th Experience Sharing Platform brought together senior aviation professionals from across Pakistan for an intensive day of knowledge exchange, case study discussions, and best practice sessions facilitated by PCAA and PAA experts.",
                Category = "CPD",
                Icon = "fas fa-plane",
                PublishDate = new DateOnly(2025, 4, 17),
                IsFeatured = true,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "12th CPD: Human Factors in Aviation Safety Engineering",
                Body = "A one-day CPD session covering human factors principles and their application to everyday aviation safety engineering practice.",
                Category = "CPD",
                Icon = "fas fa-shield-halved",
                PublishDate = new DateOnly(2025, 2, 20),
                IsFeatured = true,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "11th CPD: Conflict Resolution & Negotiation",
                Body = "Participants explored practical negotiation and conflict-resolution techniques for aviation operational and management settings.",
                Category = "CPD",
                Icon = "fas fa-tools",
                PublishDate = new DateOnly(2024, 11, 14),
                IsFeatured = true,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "Fall 2026 Admissions Now Open — Limited Seats",
                Body = "Applications for the Fall 2026 intake are now open across all NAU programs. Seats are limited and offered on a first-come, first-served basis.",
                Category = "Admissions",
                Icon = "fas fa-graduation-cap",
                PublishDate = new DateOnly(2026, 1, 15),
                IsFeatured = true,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "NAU Students Win National Aviation Quiz Competition 2026",
                Body = "A team of four NAU students secured first place at the Inter-University Aviation Quiz held in Karachi, competing against 18 institutions nationwide.",
                Category = "Academic",
                Icon = "fas fa-award",
                PublishDate = new DateOnly(2026, 3, 12),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "Annual Aviation Safety Seminar Draws 200+ Attendees",
                Body = "Experts from PCAA, PIA, and AirBlue addressed safety trends, regulatory updates, and new ICAO standards in front of students and industry guests.",
                Category = "Event",
                Icon = "fas fa-plane-departure",
                PublishDate = new DateOnly(2026, 2, 28),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "Merit List for Spring 2026 Undergraduate Intake Published",
                Body = "The Academics Office has released the merit list for all undergraduate programs. Shortlisted candidates must report for document verification by Feb 10.",
                Category = "Admissions",
                Icon = "fas fa-file-alt",
                PublishDate = new DateOnly(2026, 1, 20),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "New Avionics Laboratory Inaugurated at NAU Hyderabad",
                Body = "The state-of-the-art avionics lab, equipped with the latest navigation and communication training systems, was inaugurated by the DG Civil Aviation Authority.",
                Category = "Academic",
                Icon = "fas fa-microscope",
                PublishDate = new DateOnly(2025, 12, 5),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "10th CPD Session: Safety Management Systems in Practice",
                Body = "Participants from eight CAA departments explored real-world SMS implementation challenges and ICAO Annex 19 compliance strategies over two intensive days.",
                Category = "CPD",
                Icon = "fas fa-chalkboard-teacher",
                PublishDate = new DateOnly(2025, 10, 10),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            },
            new NewsPost
            {
                Title = "AFF Refresher Course No. 10 Completed with 32 Graduates",
                Body = "Thirty-two aerodrome firefighting officers successfully completed the AFF Refresher Course and received PCAA-endorsed certificates in a graduation ceremony.",
                Category = "Training",
                Icon = "fas fa-fire-extinguisher",
                PublishDate = new DateOnly(2025, 9, 18),
                IsFeatured = false,
                CreatedAt = DateTime.Now
            }
        );
        seedDb.SaveChanges();
    }
}

// Fix-up: the Homepage content (Pages table, slug="home") was originally saved
// with its "News & Media" dropdown links pointing at the old static
// news-media.html#... anchors from the pre-conversion design. That file/route
// doesn't exist in this app anymore -- News & Media now lives at
// /NewsPosts/Public (with #latest-news / #photo-gallery / #video-gallery
// sections on that same page) -- so those 3 links were dead. This runs on
// every startup but only touches the row if the old links are still present,
// so it's safe to leave in and won't overwrite any other homepage edits.
using (var linkFixScope = app.Services.CreateScope())
{
    var fixDb = linkFixScope.ServiceProvider.GetRequiredService<NauDbContext>();
    var homePage = fixDb.Pages.FirstOrDefault(p => p.Slug == "home");
    if (homePage != null && !string.IsNullOrEmpty(homePage.ContentHtml))
    {
        bool changed = false;
        if (homePage.ContentHtml.Contains("news-media.html"))
        {
            homePage.ContentHtml = homePage.ContentHtml
                .Replace("news-media.html#latest-news", "/NewsPosts/Public#latest-news")
                .Replace("news-media.html#photo-gallery", "/NewsPosts/Public#photo-gallery")
                .Replace("news-media.html#video-gallery", "/NewsPosts/Public#video-gallery")
                .Replace("news-media.html", "/NewsPosts/Public"); // catch-all for any bare link with no #section
            changed = true;
        }

        var signaturePattern = @"<svg\s+class=""signature""[\s\S]*?</svg>\s*<a\s+href=""#""\s+class=""btn-green"">Read More</a>";
        var newRegardsHtml = @"<div class=""rector-regards"" style=""margin-top:20px; font-weight:600; font-size:14px; color:#0b2c4d; line-height:1.6;"">Regards,<br><br><strong>Sumair Saeed</strong><br><span style=""font-weight:500; color:#64748b;"">Director, Civil Aviation Training Institute.</span></div>";

        if (System.Text.RegularExpressions.Regex.IsMatch(homePage.ContentHtml, signaturePattern))
        {
            homePage.ContentHtml = System.Text.RegularExpressions.Regex.Replace(homePage.ContentHtml, signaturePattern, newRegardsHtml);
            changed = true;
        }
        else if (homePage.ContentHtml.Contains(@"<a href=""#"" class=""btn-green"">Read More</a>"))
        {
            homePage.ContentHtml = homePage.ContentHtml.Replace(@"<a href=""#"" class=""btn-green"">Read More</a>", newRegardsHtml);
            changed = true;
        }

        // Clean up any old static programs / faculty-grid blocks and stray faculty-cards from DB so stored HTML is pristine
        var html = homePage.ContentHtml;
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<section\s+class=""(faculties|programs)""[\s\S]*?</section>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<div\s+class=""(faculty-grid|faculties-grid)""[\s\S]*?</div>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<a\s+[^>]*?(faculty-card|/Faculties/Detail)[^>]*>[\s\S]*?</a>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<a\s+[^>]*?>[\s\S]*?Faculty of (Engineering|IT|Aviation|Management)[\s\S]*?</a>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        html = System.Text.RegularExpressions.Regex.Replace(html, @"<div\s+class=""faculty-card""[^>]*?>[\s\S]*?</div>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var factsTemplate = @"<section class=""facts2"">
  <div class=""facts-inner"">
    <div class=""section-head"" style=""max-width:1380px;margin:0 auto 0;border-bottom:2px solid var(--green);padding-bottom:14px;margin-bottom:30px;justify-content:center;"">
      <h3 style=""font-family:var(--font-head);font-size:18px;font-weight:700;color:var(--navy);text-transform:uppercase;letter-spacing:0.5px;"">FACTS &amp; FIGURES</h3>
    </div>
    <div class=""facts-grid"">
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-university""></i></div>
        <div class=""fact-number"">2026</div>
        <div class=""fact-label"">Year of Establishment</div>
      </div>
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-user-graduate""></i></div>
        <div class=""fact-number"">19935</div>
        <div class=""fact-label"">Students Enrolled</div>
      </div>
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-book-open""></i></div>
        <div class=""fact-number"">2474</div>
        <div class=""fact-label"">Total Courses</div>
      </div>
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-chart-line""></i></div>
        <div class=""fact-number"">23313</div>
        <div class=""fact-label"">G. Total</div>
      </div>
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-briefcase""></i></div>
        <div class=""fact-number"">2126</div>
        <div class=""fact-label"">Govt/Corporate</div>
      </div>
      <div class=""fact-item"">
        <div class=""fact-icon""><i class=""fas fa-globe""></i></div>
        <div class=""fact-number"">49</div>
        <div class=""fact-label"">Number of States which have sent trainees to CATI</div>
      </div>
    </div>
  </div>
</section>";

        if (!html.Contains("facts2"))
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(html, @"<section\s+class=""news-events""[\s\S]*?</section>", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                html = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    @"<section\s+class=""news-events""[\s\S]*?</section>",
                    "$0" + factsTemplate,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            else
            {
                html += factsTemplate;
            }
        }

        if (html != homePage.ContentHtml)
        {
            homePage.ContentHtml = html;
            changed = true;
        }

        if (changed)
        {
            homePage.UpdatedAt = DateTime.UtcNow;
            fixDb.SaveChanges();
        }
    }

    // Ensure IsActive, IsSplashScreen, and ExpiresAt columns exist in NewsPosts & Events tables
    try
    {
        fixDb.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NewsPosts') AND name = 'IsActive')
            BEGIN
                ALTER TABLE NewsPosts ADD IsActive BIT NOT NULL CONSTRAINT DF_NewsPosts_IsActive DEFAULT 1;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NewsPosts') AND name = 'IsSplashScreen')
            BEGIN
                ALTER TABLE NewsPosts ADD IsSplashScreen BIT NOT NULL CONSTRAINT DF_NewsPosts_IsSplashScreen DEFAULT 0;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NewsPosts') AND name = 'ExpiresAt')
            BEGIN
                ALTER TABLE NewsPosts ADD ExpiresAt DATE NULL;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'ExpiresAt')
            BEGIN
                ALTER TABLE Events ADD ExpiresAt DATE NULL;
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'IsActive')
            BEGIN
                ALTER TABLE Events ADD IsActive BIT NOT NULL CONSTRAINT DF_Events_IsActive DEFAULT 1;
            END;
        ");

        // Advance dates of existing news posts and events into 2026/2027 and set expiry dates
        var now = DateOnly.FromDateTime(DateTime.Now);
        var baseDate = new DateOnly(2026, 8, 20);
        bool newsUpdated = false;

        var newsPosts = fixDb.NewsPosts.ToList();
        foreach (var np in newsPosts)
        {
            if (np.PublishDate < baseDate)
            {
                np.PublishDate = baseDate.AddDays(-Random.Shared.Next(1, 45));
                newsUpdated = true;
            }
            if (!np.ExpiresAt.HasValue || np.ExpiresAt.Value <= now)
            {
                np.ExpiresAt = np.PublishDate.AddMonths(12);
                newsUpdated = true;
            }
        }

        if (!fixDb.Events.Any())
        {
            fixDb.Events.AddRange(
                new Event
                {
                    Title = "Aviation Safety & Flight Operations Seminar 2026",
                    EventDate = new DateOnly(2026, 9, 15),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(13, 0),
                    Location = "NAU Auditorium, Main Campus",
                    IsActive = true,
                    ExpiresAt = new DateOnly(2027, 9, 15)
                },
                new Event
                {
                    Title = "Advanced Aircraft Maintenance & Avionics Workshop",
                    EventDate = new DateOnly(2026, 9, 28),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(15, 0),
                    Location = "Hangar Training Facility",
                    IsActive = true,
                    ExpiresAt = new DateOnly(2027, 9, 28)
                },
                new Event
                {
                    Title = "Aviation Career Day & Industry Interaction 2026",
                    EventDate = new DateOnly(2026, 10, 12),
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(14, 0),
                    Location = "NAU Campus Grounds",
                    IsActive = true,
                    ExpiresAt = new DateOnly(2027, 10, 12)
                },
                new Event
                {
                    Title = "Annual Aviation Excellence Gala & Convocation Prep",
                    EventDate = new DateOnly(2026, 11, 14),
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(9, 30),
                    Location = "Main Parade Ground, NAU",
                    IsActive = true,
                    ExpiresAt = new DateOnly(2027, 11, 14)
                }
            );
            newsUpdated = true;
        }

        var eventsList = fixDb.Events.ToList();
        foreach (var ev in eventsList)
        {
            if (ev.EventDate < baseDate)
            {
                ev.EventDate = baseDate.AddDays(Random.Shared.Next(15, 90));
                newsUpdated = true;
            }
            if (!ev.ExpiresAt.HasValue || ev.ExpiresAt.Value <= now)
            {
                ev.ExpiresAt = ev.EventDate.AddMonths(2);
                newsUpdated = true;
            }
            ev.IsActive = true;
        }

        if (newsUpdated)
        {
            fixDb.SaveChanges();
        }
    }
    catch { /* Ignore if already exists or handled */ }

    // Facilities content & tags cleanup: Update "Masjid" -> "Mosque" and "Free Wi-Fi" -> "Wi-Fi" in database records
    try
    {
        var allPages = fixDb.Pages.ToList();
        bool pagesChanged = false;
        foreach (var p in allPages)
        {
            if (!string.IsNullOrEmpty(p.ContentHtml))
            {
                var updated = p.ContentHtml
                    .Replace("Free Wi-Fi", "Wi-Fi")
                    .Replace("Free Wifi", "Wi-Fi")
                    .Replace("free wi-fi", "wi-fi")
                    .Replace("free wifi", "wifi")
                    .Replace("Central Masjid", "Central Mosque");

                if (updated != p.ContentHtml)
                {
                    p.ContentHtml = updated;
                    p.UpdatedAt = DateTime.UtcNow;
                    pagesChanged = true;
                }
            }
        }
        if (pagesChanged)
        {
            fixDb.SaveChanges();
        }

        var cardTags = fixDb.FacilityCardTags.ToList();
        bool tagsChanged = false;
        foreach (var tag in cardTags)
        {
            if (!string.IsNullOrEmpty(tag.Label))
            {
                var original = tag.Label;
                if (tag.Label.Equals("Free Wi-Fi", StringComparison.OrdinalIgnoreCase) || tag.Label.Equals("Free Wifi", StringComparison.OrdinalIgnoreCase))
                {
                    tag.Label = "Wi-Fi";
                    tagsChanged = true;
                }
                else if (tag.Label.Contains("Masjid", StringComparison.OrdinalIgnoreCase))
                {
                    tag.Label = tag.Label.Replace("Masjid", "Mosque", StringComparison.OrdinalIgnoreCase);
                    tagsChanged = true;
                }
            }
        }
        if (tagsChanged)
        {
            fixDb.SaveChanges();
        }

        var facilitiesList = fixDb.Facilities.ToList();
        bool facsChanged = false;
        foreach (var fac in facilitiesList)
        {
            if (fac.Name != null && fac.Name.Contains("Masjid", StringComparison.OrdinalIgnoreCase))
            {
                fac.Name = fac.Name.Replace("Masjid", "Mosque", StringComparison.OrdinalIgnoreCase);
                facsChanged = true;
            }
            if (fac.IntroText != null && (fac.IntroText.Contains("Free Wi-Fi", StringComparison.OrdinalIgnoreCase) || fac.IntroText.Contains("Masjid", StringComparison.OrdinalIgnoreCase)))
            {
                fac.IntroText = fac.IntroText
                    .Replace("Free Wi-Fi", "Wi-Fi", StringComparison.OrdinalIgnoreCase)
                    .Replace("Free Wifi", "Wi-Fi", StringComparison.OrdinalIgnoreCase)
                    .Replace("Masjid", "Mosque", StringComparison.OrdinalIgnoreCase);
                facsChanged = true;
            }
        }
        if (facsChanged)
        {
            fixDb.SaveChanges();
        }

        // Financial Page Banks Logo URL Update
        var finPage = fixDb.Pages.FirstOrDefault(p => p.Slug == "financial");
        if (finPage != null && !string.IsNullOrWhiteSpace(finPage.ContentHtml))
        {
            try
            {
                var finData = System.Text.Json.JsonSerializer.Deserialize<FinancialPageContent>(finPage.ContentHtml);
                if (finData != null && finData.Banks != null && finData.Banks.Any())
                {
                    bool finChanged = false;

                    // 1. Separate combined Easypaisa / JazzCash
                    var comb = finData.Banks.FirstOrDefault(b => (b.Name ?? "").Contains("Easypaisa", StringComparison.OrdinalIgnoreCase) && (b.Name ?? "").Contains("Jazz", StringComparison.OrdinalIgnoreCase));
                    if (comb != null)
                    {
                        comb.Name = "Easypaisa";
                        comb.Details = "Mobile wallet payment via challan number & Easypaisa App";
                        comb.TagText = "Mobile";
                        comb.TagBg = "#e6f4ea";
                        comb.TagColor = "#1e7e34";
                        comb.LogoText = "EP";
                        comb.LogoBgHex = "#00a859";
                        comb.LogoUrl = "/images/banks/easypaisa.png";
                        finChanged = true;
                    }

                    // 2. Ensure JazzCash item exists
                    if (!finData.Banks.Any(b => (b.Name ?? "").Equals("JazzCash", StringComparison.OrdinalIgnoreCase) || (b.Name ?? "").StartsWith("Jazz", StringComparison.OrdinalIgnoreCase)))
                    {
                        finData.Banks.Add(new BankData
                        {
                            Id = "bank-6",
                            Name = "JazzCash",
                            Details = "Mobile wallet payment & retail shop agents",
                            TagText = "Mobile",
                            TagBg = "#fff7e6",
                            TagColor = "#c87800",
                            LogoText = "JAZZ",
                            LogoBgHex = "#d81e05",
                            LogoUrl = "/images/banks/jazzcash.png"
                        });
                        finChanged = true;
                    }

                    // 3. Update all logo URLs
                    foreach (var b in finData.Banks)
                    {
                        var n = (b.Name ?? "").ToLowerInvariant();
                        var l = (b.LogoText ?? "").ToLowerInvariant();
                        if (n.Contains("hbl") || n.Contains("habib") || l == "hbl")
                        {
                            if (b.LogoUrl != "/images/banks/hbl.png") { b.LogoUrl = "/images/banks/hbl.png"; finChanged = true; }
                            b.LogoBgHex = "#006b52";
                        }
                        else if (n.Contains("nbp") || n.Contains("national bank") || l == "nbp")
                        {
                            if (b.LogoUrl != "/images/banks/nbp.png") { b.LogoUrl = "/images/banks/nbp.png"; finChanged = true; }
                            b.LogoBgHex = "#006039";
                        }
                        else if (n.Contains("mcb") || l == "mcb")
                        {
                            if (b.LogoUrl != "/images/banks/mcb.png") { b.LogoUrl = "/images/banks/mcb.png"; finChanged = true; }
                            b.LogoBgHex = "#ffffff";
                        }
                        else if (n.Contains("ubl") || n.Contains("united bank") || l == "ubl")
                        {
                            if (b.LogoUrl != "/images/banks/ubl.png") { b.LogoUrl = "/images/banks/ubl.png"; finChanged = true; }
                            b.LogoBgHex = "#005baa";
                        }
                        else if (n.Contains("easypaisa") || l == "easy" || l == "ep")
                        {
                            if (b.Name != "Easypaisa") { b.Name = "Easypaisa"; finChanged = true; }
                            if (b.LogoUrl != "/images/banks/easypaisa.png") { b.LogoUrl = "/images/banks/easypaisa.png"; finChanged = true; }
                            b.LogoBgHex = "#00a859";
                        }
                        else if (n.Contains("jazz") || l == "jazz")
                        {
                            if (b.Name != "JazzCash") { b.Name = "JazzCash"; finChanged = true; }
                            if (b.LogoUrl != "/images/banks/jazzcash.png") { b.LogoUrl = "/images/banks/jazzcash.png"; finChanged = true; }
                            b.LogoBgHex = "#d81e05";
                        }
                    }
                    if (finChanged)
                    {
                        finPage.ContentHtml = System.Text.Json.JsonSerializer.Serialize(finData);
                        finPage.UpdatedAt = DateTime.UtcNow;
                        fixDb.SaveChanges();
                    }
                }
            }
            catch { /* Ignore serialization issues */ }
        }
    }
    catch { /* Ignore database access errors during startup */ }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Real-Time Visitor Analytics Tracking Middleware
app.UseMiddleware<NAU_Web.Middleware.VisitorAnalyticsMiddleware>();

app.MapHealthChecks("/health");

app.MapGet("/health/json", async (ISiteUptimeMonitorService monitor) =>
{
    var data = await monitor.GetDashboardDataAsync();
    return Results.Json(new
    {
        status = data.Status,
        database = data.DatabaseHealthy ? "Healthy" : "Unhealthy",
        databaseLatencyMs = data.DatabaseLatencyMs,
        uptime = data.ServerUptime.ToString(@"dd\.hh\:mm\:ss"),
        memoryUsageMb = data.MemoryUsageMb,
        diskFreeGb = data.DiskFreeGb,
        timestamp = DateTime.UtcNow
    });
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

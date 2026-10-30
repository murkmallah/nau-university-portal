using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;
using System.Diagnostics;

namespace NAU_Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;
        private readonly ISchoolService _schoolService;

        public const string HomeSlug = "home";

        public HomeController(
            ILogger<HomeController> logger,
            NauDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            IUserPermissionService permissionService,
            ISchoolService schoolService)
        {
            _logger = logger;
            _context = context;
            _env = env;
            _userManager = userManager;
            _permissionService = permissionService;
            _schoolService = schoolService;
        }

        public async Task<IActionResult> Index(string? edit = null)
        {
            bool wantsEdit = !string.IsNullOrEmpty(edit) &&
                (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

            ApplicationUser? currentUser = null;
            bool hasHomePerm = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    hasHomePerm = await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentHomepage)
                               || await _permissionService.IsSuperAdminAsync(currentUser.Id);
                }
            }

            bool canEdit = currentUser != null && (User.IsInRole("Admin") || hasHomePerm);
            ViewBag.CanEdit = canEdit;
            ViewBag.EditMode = wantsEdit && canEdit;

            ViewBag.CurrentUserName = "";
            if (currentUser != null)
            {
                ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity?.Name ?? "");
            }

            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == HomeSlug);

            if (page == null)
            {
                // First run: seed the homepage content from the bundled seed file
                var seedPath = Path.Combine(_env.ContentRootPath, "SeedData", "home-content.html");
                var seedHtml = System.IO.File.Exists(seedPath)
                    ? await System.IO.File.ReadAllTextAsync(seedPath)
                    : "<p>Homepage content not found. Please contact the administrator.</p>";

                page = new Page
                {
                    Slug = HomeSlug,
                    Title = "National Aviation University - Home",
                    ContentHtml = seedHtml,
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Pages.Add(page);
                await _context.SaveChangesAsync();
            }

            // One-time (self-healing) fix: earlier saves of the homepage baked
            // the announcement bar and/or navbar directly into the stored
            // ContentHtml (the CMS "Save" button used to capture the entire
            // editable region, header included -- now fixed in cms-editor.js,
            // but content already saved with the old bug needs to be cleaned
            // up too). Strip any such leftover block(s) from the very start
            // of the stored content so it never renders twice next to the
            // real, centrally-styled announcement bar / navbar partials.
            // Runs as a no-op once nothing leftover is found, so it's safe on
            // every later page load.
            if (!string.IsNullOrEmpty(page.ContentHtml))
            {
                var strippedHeader = StripLeadingLegacyHeaderBlocks(page.ContentHtml);
                if (strippedHeader != page.ContentHtml)
                {
                    page.ContentHtml = strippedHeader;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time self-healing fix: remove any static dots baked into #slideDots in the database
            if (!string.IsNullOrEmpty(page.ContentHtml) && page.ContentHtml.Contains("slideDots"))
            {
                var cleanedDots = System.Text.RegularExpressions.Regex.Replace(
                    page.ContentHtml,
                    @"<div\s+class=""slide-dots""\s+id=""slideDots"">[\s\S]*?</div>",
                    @"<div class=""slide-dots"" id=""slideDots""></div>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (cleanedDots != page.ContentHtml)
                {
                    page.ContentHtml = cleanedDots;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time self-healing fix: remove any legacy .cms-slide-action-bar from database
            if (!string.IsNullOrEmpty(page.ContentHtml) && page.ContentHtml.Contains("cms-slide-action-bar"))
            {
                var cleanedBars = System.Text.RegularExpressions.Regex.Replace(
                    page.ContentHtml,
                    @"<div\s+class=""cms-slide-action-bar""[^>]*>[\s\S]*?</div>",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (cleanedBars != page.ContentHtml)
                {
                    page.ContentHtml = cleanedBars;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time self-healing fix: ensure default image slides have explicit inline background-images
            // so removing/editing slides doesn't break due to missing CSS rules.
            if (!string.IsNullOrEmpty(page.ContentHtml) && page.ContentHtml.Contains("<div class=\"hero-slide\"><div class=\"slide-bg-inner\"></div></div>"))
            {
                var defaultImages = new[]
                {
                    "https://catipakistan.com.pk/mainImages/select/DES04003.jpg",
                    "https://catipakistan.com.pk/images/HomeSlider/83437f3b-6d54-4d03-ba36-640c19aa278e_6.JPG",
                    "https://catipakistan.com.pk/images/HomeSlider/f431b3d4-43d1-43f6-86b2-381d48c71c16_8.jpg",
                    "https://catipakistan.com.pk/mainImages/select/DES03254.jpg"
                };

                var patchedHtml = page.ContentHtml;
                for (int i = 0; i < defaultImages.Length; i++)
                {
                    const string bareSlide = "<div class=\"hero-slide\"><div class=\"slide-bg-inner\"></div></div>";
                    int idx = patchedHtml.IndexOf(bareSlide, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var replacement = $"<div class=\"hero-slide\" style=\"background-image: url('{defaultImages[i]}');\"><div class=\"slide-bg-inner\"></div></div>";
                        patchedHtml = patchedHtml.Substring(0, idx) + replacement + patchedHtml.Substring(idx + bareSlide.Length);
                    }
                }

                if (patchedHtml != page.ContentHtml)
                {
                    page.ContentHtml = patchedHtml;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time fix: the homepage content (saved in the database) has
            // the "Faculty of IT & Cyber Security" nav item pointing at a
            // dead static link, or at the generic all-faculties grid --
            // either way it should be its OWN dedicated page. Match the
            // anchor by its icon + label (regardless of whatever href it
            // currently has, old or already-patched) and point it at
            // /Faculties/Detail/{slug}. Runs as a no-op once the link
            // already matches, so it's safe on every later page load.
            var itLinkPattern = new System.Text.RegularExpressions.Regex(
                "<a href=\"[^\"]*\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-laptop-code\"></i></span><span class=\"dd-text\"><strong>Faculty of IT &amp; Cyber Security </strong>");
            const string itLinkReplacement =
                "<a href=\"/Faculties/Detail/information-technology-cyber-security\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-laptop-code\"></i></span><span class=\"dd-text\"><strong>Faculty of IT &amp; Cyber Security </strong>";
            if (!string.IsNullOrEmpty(page.ContentHtml))
            {
                var patched = itLinkPattern.Replace(page.ContentHtml, itLinkReplacement);
                if (patched != page.ContentHtml)
                {
                    page.ContentHtml = patched;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time fix: add a "View All Links" button to the "Useful
            // Links" dropdown (homepage content stored in DB) so visitors
            // can reach the new /links.html page, alongside the existing
            // LMS / E-library links. Runs as a no-op once the button is
            // already present, so it's safe on every later page load.
            const string usefulLinksMarker =
                "<a href=\"https://cati-elibrary.pitb.gov.pk/\" target=\"_blank\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-landmark\"></i></span><span class=\"dd-text\"><strong>E-library</strong></span></a>";
            const string viewAllLinksAnchor =
                "<a href=\"/links.html\" class=\"dd-view-all\">View All Links <i class=\"fas fa-arrow-right\"></i></a>";
            // Older/plain version of the button, in case an earlier run of
            // this fix already inserted it -- upgrade it to the green
            // dd-view-all style instead of adding a second entry.
            const string oldViewAllLinksAnchor =
                "<a href=\"/links.html\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-link\"></i></span><span class=\"dd-text\"><strong>View All Links</strong></span></a>";

            if (!string.IsNullOrEmpty(page.ContentHtml) && page.ContentHtml.Contains(oldViewAllLinksAnchor))
            {
                page.ContentHtml = page.ContentHtml.Replace(oldViewAllLinksAnchor, viewAllLinksAnchor);
                page.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(page.ContentHtml)
                && page.ContentHtml.Contains(usefulLinksMarker)
                && !page.ContentHtml.Contains(viewAllLinksAnchor))
            {
                page.ContentHtml = page.ContentHtml.Replace(usefulLinksMarker, usefulLinksMarker + viewAllLinksAnchor);
                page.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // One-time fix: rename the "Short Courses" column in the
            // "Academics" dropdown (homepage content stored in DB) to
            // "Schools", and update its "View All Short Courses" button to
            // say "View All Schools" to match. Runs as a no-op once the
            // wording already matches, so it's safe on every later page load.
            const string shortCoursesTitle = "<div class=\"dd-col-title\">Short Courses</div>";
            const string schoolsTitle = "<div class=\"dd-col-title\">Schools</div>";
            const string viewAllShortCourses = "View All Short Courses <i class=\"fas fa-arrow-right\"></i>";
            const string viewAllSchools = "View All Schools <i class=\"fas fa-arrow-right\"></i>";

            if (!string.IsNullOrEmpty(page.ContentHtml) &&
                (page.ContentHtml.Contains(shortCoursesTitle) || page.ContentHtml.Contains(viewAllShortCourses) || page.ContentHtml.Contains("View All Programs")))
            {
                page.ContentHtml = page.ContentHtml
                    .Replace(shortCoursesTitle, schoolsTitle)
                    .Replace(viewAllShortCourses, viewAllSchools)
                    .Replace("View All Programs", "View All Faculties");
                page.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // One-time fix: the "Schools" column in the "Academics" dropdown
            // (homepage content stored in DB) was still listing 4 individual
            // short courses instead of the 4 actual Schools. Swap each old
            // course link for the matching school link, deep-linking into
            // /Courses/Public?school=... so clicking a school opens straight
            // to that school's accordion. Runs as a no-op once each link
            // already matches, so it's safe on every later page load.
            var schoolLinkSwaps = new (string Old, string New)[]
            {
                (
                    "<a href=\"/Courses/Public\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-search\"></i></span><span class=\"dd-text\"><strong>Audit Technique Course No. 03</strong><span>Aviation audit methods and best practices</span></span></a>",
                    "<a href=\"/Courses/Public?school=navigation\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-broadcast-tower\"></i></span><span class=\"dd-text\"><strong>School of Air Navigation Services (ANS)</strong><span>Air Traffic Control &amp; CNS/ATM Systems</span></span></a>"
                ),
                (
                    "<a href=\"/Courses/Public\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-headset\"></i></span><span class=\"dd-text\"><strong>ATC Assistant Course No. 02</strong><span>Air traffic control assistant training</span></span></a>",
                    "<a href=\"/Courses/Public?school=regulatory\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-balance-scale\"></i></span><span class=\"dd-text\"><strong>School of Regulatory Services</strong><span>Aviation Safety &amp; Regulatory Compliance</span></span></a>"
                ),
                (
                    "<a href=\"/Courses/Public\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-wind\"></i></span><span class=\"dd-text\"><strong>HVAC Operations &amp; Maintenance Course No. 02</strong><span>Heating, ventilation and A/C systems</span></span></a>",
                    "<a href=\"/Courses/Public?school=airport\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-fire-extinguisher\"></i></span><span class=\"dd-text\"><strong>School of Airport Services</strong><span>Ground Operations &amp; Rescue/Firefighting</span></span></a>"
                ),
                (
                    "<a href=\"/Courses/Public\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-redo\"></i></span><span class=\"dd-text\"><strong>AFF Refresher Course No. 11</strong><span>Aerodrome firefighting refresher</span></span></a>",
                    "<a href=\"/Courses/Public?school=management\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-briefcase\"></i></span><span class=\"dd-text\"><strong>School of Aviation Management</strong><span>Leadership &amp; Management Training</span></span></a>"
                ),
            };

            if (!string.IsNullOrEmpty(page.ContentHtml))
            {
                var patchedSchools = page.ContentHtml;
                foreach (var (oldLink, newLink) in schoolLinkSwaps)
                {
                    patchedSchools = patchedSchools.Replace(oldLink, newLink);
                }
                if (patchedSchools != page.ContentHtml)
                {
                    page.ContentHtml = patchedSchools;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            // One-time fix: give each school link in the "Schools" dropdown
            // column (homepage content stored in DB) a small arrow that
            // expands a live course preview, matching the other pages.
            // Wraps each existing school anchor in a .dd-school-item block
            // with a .dd-submenu placeholder (populated client-side by
            // schools-submenu.js via /Courses/BySchool). Runs as a no-op
            // once a link is already wrapped, so it's safe on every later
            // page load.
            var schoolSubmenuWraps = new (string SchoolKey, string OldLink, string NewLink)[]
            {
                (
                    "navigation",
                    "<a href=\"/Courses/Public?school=navigation\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-broadcast-tower\"></i></span><span class=\"dd-text\"><strong>School of Air Navigation Services (ANS)</strong><span>Air Traffic Control &amp; CNS/ATM Systems</span></span></a>",
                    "<a href=\"/Courses/Public?school=navigation\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-broadcast-tower\"></i></span><span class=\"dd-text\"><strong>School of Air Navigation Services (ANS)</strong><span>Air Traffic Control &amp; CNS/ATM Systems</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>"
                ),
                (
                    "regulatory",
                    "<a href=\"/Courses/Public?school=regulatory\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-balance-scale\"></i></span><span class=\"dd-text\"><strong>School of Regulatory Services</strong><span>Aviation Safety &amp; Regulatory Compliance</span></span></a>",
                    "<a href=\"/Courses/Public?school=regulatory\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-balance-scale\"></i></span><span class=\"dd-text\"><strong>School of Regulatory Services</strong><span>Aviation Safety &amp; Regulatory Compliance</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>"
                ),
                (
                    "airport",
                    "<a href=\"/Courses/Public?school=airport\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-fire-extinguisher\"></i></span><span class=\"dd-text\"><strong>School of Airport Services</strong><span>Ground Operations &amp; Rescue/Firefighting</span></span></a>",
                    "<a href=\"/Courses/Public?school=airport\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-fire-extinguisher\"></i></span><span class=\"dd-text\"><strong>School of Airport Services</strong><span>Ground Operations &amp; Rescue/Firefighting</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>"
                ),
                (
                    "management",
                    "<a href=\"/Courses/Public?school=management\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-briefcase\"></i></span><span class=\"dd-text\"><strong>School of Aviation Management</strong><span>Leadership &amp; Management Training</span></span></a>",
                    "<a href=\"/Courses/Public?school=management\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-briefcase\"></i></span><span class=\"dd-text\"><strong>School of Aviation Management</strong><span>Leadership &amp; Management Training</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>"
                ),
            };

            if (!string.IsNullOrEmpty(page.ContentHtml))
            {
                var patchedSubmenu = page.ContentHtml;
                foreach (var (schoolKey, oldLink, newLink) in schoolSubmenuWraps)
                {
                    if (patchedSubmenu.Contains(oldLink))
                    {
                        var wrapped = "<div class=\"dd-school-item\">" + newLink +
                            "<div class=\"dd-submenu\" data-school=\"" + schoolKey + "\"></div></div>";
                        patchedSubmenu = patchedSubmenu.Replace(oldLink, wrapped);
                    }
                }
                if (patchedSubmenu != page.ContentHtml)
                {
                    page.ContentHtml = patchedSubmenu;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.Faculties = await _context.Faculties.ToListAsync();
            ViewBag.Schools = await _schoolService.GetActiveSchoolsAsync();

            var today = DateOnly.FromDateTime(DateTime.Now);
            ViewBag.LatestNews = await _context.NewsPosts
                .Where(n => n.IsActive && (n.ExpiresAt == null || n.ExpiresAt >= today))
                .OrderByDescending(n => n.PublishDate)
                .ThenByDescending(n => n.NewsPostId)
                .Take(6)
                .ToListAsync();

            ViewBag.UpcomingEvents = await _context.Events
                .Where(e => e.IsActive && (e.ExpiresAt == null || e.ExpiresAt >= today))
                .OrderBy(e => e.EventDate)
                .ThenBy(e => e.EventId)
                .Take(6)
                .ToListAsync();

            return View(page);
        }

        // Removes a leftover leading <div class="announcement">...</div> and/or
        // <nav class="navbar">...</nav> / <div class="navbar">...</div> block
        // from the start of the saved homepage content (older saves used a
        // <div> for the navbar, newer ones use a real <nav> tag -- this
        // handles both, and repeats in case multiple copies got stacked over
        // several edits). Leaves everything else untouched.
        private static string StripLeadingLegacyHeaderBlocks(string html)
        {
            if (string.IsNullOrEmpty(html)) return html;

            for (int safety = 0; safety < 10; safety++)
            {
                int idx = 0;
                while (idx < html.Length && char.IsWhiteSpace(html[idx])) idx++;
                if (idx >= html.Length) break;

                var rest = html.Substring(idx);
                string tagName;
                if (rest.StartsWith("<div class=\"announcement\"", StringComparison.OrdinalIgnoreCase))
                {
                    tagName = "div";
                }
                else if (rest.StartsWith("<nav class=\"navbar\"", StringComparison.OrdinalIgnoreCase))
                {
                    tagName = "nav";
                }
                else if (rest.StartsWith("<div class=\"navbar\"", StringComparison.OrdinalIgnoreCase))
                {
                    tagName = "div";
                }
                else
                {
                    break; // nothing leftover to strip
                }

                int end = FindMatchingCloseTagEnd(html, idx, tagName);
                if (end <= idx) break; // malformed / couldn't find a safe match -- stop rather than corrupt content

                html = html.Substring(0, idx) + html.Substring(end);
            }

            return html;
        }

        // Given the index of a '<' that starts an opening tag (e.g. the '<' in
        // <div class="...">), returns the index just after that tag's matching
        // closing tag, correctly skipping over any same-named tags nested inside it.
        private static int FindMatchingCloseTagEnd(string html, int openTagStart, string tagName)
        {
            int openTagEndIdx = html.IndexOf('>', openTagStart);
            if (openTagEndIdx < 0) return -1;

            string openPattern = "<" + tagName;
            string closePattern = "</" + tagName + ">";
            int pos = openTagEndIdx + 1;
            int depth = 1;

            while (depth > 0)
            {
                int nextClose = html.IndexOf(closePattern, pos, StringComparison.OrdinalIgnoreCase);
                if (nextClose < 0) return -1; // malformed -- bail out safely

                int nextOpen = html.IndexOf(openPattern, pos, StringComparison.OrdinalIgnoreCase);
                bool isRealOpen = false;
                if (nextOpen >= 0 && nextOpen < nextClose)
                {
                    int afterIdx = nextOpen + openPattern.Length;
                    char after = afterIdx < html.Length ? html[afterIdx] : ' ';
                    isRealOpen = after == ' ' || after == '>' || after == '\t' || after == '\r' || after == '\n' || after == '/';
                }

                if (isRealOpen)
                {
                    depth++;
                    pos = nextOpen + openPattern.Length;
                }
                else
                {
                    depth--;
                    pos = nextClose + closePattern.Length;
                }
            }

            return pos;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

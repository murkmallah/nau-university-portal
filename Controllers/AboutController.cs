using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    // Serves the public "About" page at the same URL it always used
    // (/about.html). The "History of CAA in Pakistan", "History of CATI",
    // AND "Management & Governance" sections are all backed by the
    // database (Pages table) so an Admin can edit them inline, exactly the
    // same way the Homepage's content is editable.
    public class AboutController : Controller
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;

        // Slug used in the Pages table for the CAA-history section content.
        public const string HistoryCaaSlug = "about-history-caa";

        // Slug used in the Pages table for the CATI-history section content.
        public const string HistoryCatiSlug = "about-history-cati";

        // Slug used in the Pages table for the Management & Governance section content.
        public const string ManagementSlug = "about-management";

        public AboutController(
            NauDbContext context,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager,
            IUserPermissionService permissionService)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _permissionService = permissionService;
        }

        // GET: /about.html
        [HttpGet("/about.html")]
        public async Task<IActionResult> Index(string? edit = null)
        {
            bool wantsEdit = !string.IsNullOrEmpty(edit) &&
                (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

            ApplicationUser? currentUser = null;
            bool canEdit = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    canEdit = await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentAbout)
                           || await _permissionService.IsSuperAdminAsync(currentUser.Id);
                }
            }

            ViewBag.EditMode = wantsEdit && canEdit;

            ViewBag.CurrentUserName = "";
            if (currentUser != null)
            {
                ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity?.Name ?? "");
            }

            // "History of CAA in Pakistan" -- Section 1 (already editable).
            var caaPage = await LoadOrSeedPageAsync(
                HistoryCaaSlug,
                "About - History of CAA in Pakistan",
                "about-history-caa-content.html");

            // "History of CATI" -- Section 2 (now editable the same way).
            var catiPage = await LoadOrSeedPageAsync(
                HistoryCatiSlug,
                "About - History of CATI",
                "about-history-cati-content.html");

            ViewBag.HistoryCatiPage = catiPage;

            // "Management & Governance" -- Section 3 (now editable the same way).
            var managementPage = await LoadOrSeedPageAsync(
                ManagementSlug,
                "About - Management & Governance",
                "about-management-content.html");

            ViewBag.ManagementPage = managementPage;

            return View(caaPage);
        }

        // Loads a Page row by slug, seeding (or re-seeding) it from the
        // matching bundled seed file when there's no row yet, OR when a row
        // exists but is empty / still holding the "not found" placeholder
        // (e.g. it was first created before the seed file was added to the
        // project). This keeps a section from getting stuck blank just
        // because it was loaded once too early. Shared by every
        // database-backed section on the About page (CAA history, CATI
        // history, ...).
        private async Task<Page> LoadOrSeedPageAsync(string slug, string title, string seedFileName)
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == slug);

            bool needsSeeding = page == null
                || string.IsNullOrWhiteSpace(page.ContentHtml)
                || page.ContentHtml.Contains("Content not found. Please contact the administrator.")
                || (slug == ManagementSlug && !page.ContentHtml.Contains("ims-policy-container"));

            if (needsSeeding)
            {
                var seedPath = Path.Combine(_env.ContentRootPath, "SeedData", seedFileName);
                var seedHtml = System.IO.File.Exists(seedPath)
                    ? await System.IO.File.ReadAllTextAsync(seedPath)
                    : "<p>Content not found. Please contact the administrator.</p>";

                if (page == null)
                {
                    page = new Page
                    {
                        Slug = slug,
                        Title = title,
                        ContentHtml = seedHtml,
                        IsPublished = true,
                        UpdatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        _context.Pages.Add(page);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        // Another request seeded the same row a split second
                        // earlier (unique Slug index) -- just load that one
                        // instead of erroring out.
                        page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == slug)
                            ?? page;
                    }
                }
                else if (!seedHtml.Contains("Content not found. Please contact the administrator."))
                {
                    // Existing empty/placeholder row -- fill it in now that
                    // real seed content is available.
                    page.ContentHtml = seedHtml;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return page!;
        }
    }
}

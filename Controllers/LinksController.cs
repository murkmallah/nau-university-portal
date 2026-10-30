using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;

namespace NAU_Web.Controllers
{
    // Public "Useful Links" page. Shows the links that appear in the
    // "Useful Links" dropdown menu (LMS + E-library + whatever admins add),
    // styled to match the rest of the site (same header/nav, hero banner,
    // and card design as pages like /Facilities/Public).
    //
    // Admin-managed: links now live in the Links table (see Models/LinkItem.cs
    // + NauDbContext.cs). Admins reach the inline add/edit/delete controls
    // only via the Admin Panel's "Useful Links" link (?edit=1) -- same rule
    // as the Short Courses / Faculties / News & Media public pages.
    [Authorize(Roles = "Admin")]
    public class LinksController : Controller
    {
        private readonly NauDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LinksController(NauDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= PUBLIC-FACING PAGE (no login required) =================
        // GET: /links.html[?edit=1]
        [AllowAnonymous]
        [HttpGet("/links.html")]
        public async Task<IActionResult> Index(string? edit = null)
        {
            bool wantsEdit = !string.IsNullOrEmpty(edit) &&
                (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

            bool isAdmin = User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");
            ViewBag.EditMode = wantsEdit && isAdmin;

            // Same pattern used on every other public page (Facilities,
            // Faculties, About, ...) so the header shows "Hi, <name>" /
            // Admin Panel correctly for a logged-in user.
            ViewBag.CurrentUserName = "";
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.CurrentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity.Name ?? "");
            }

            var links = await _context.Links
                .OrderBy(l => l.DisplayOrder)
                .ThenBy(l => l.LinkId)
                .ToListAsync();

            return View(links);
        }

        // ================= ADMIN-ONLY AJAX ENDPOINTS (Edit Mode only) =================

        public class QuickAddLinkRequest
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Url { get; set; }
            public string? Icon { get; set; }
        }

        // POST: /Links/QuickAdd  (Admin only, Edit Mode only -- lets the live
        // "+ Add Link" button create a real Link row without leaving the page.)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd([FromBody] QuickAddLinkRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Url))
            {
                return BadRequest(new { success = false, message = "Link name and URL are both required." });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var maxOrder = await _context.Links.Select(l => (int?)l.DisplayOrder).MaxAsync() ?? 0;

            var link = new LinkItem
            {
                Name = req.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(req.Description) ? "" : req.Description.Trim(),
                Url = req.Url.Trim(),
                Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-link" : req.Icon.Trim(),
                DisplayOrder = maxOrder + 1,
                CreatedByUserId = currentUser?.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Links.Add(link);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                linkId = link.LinkId,
                name = link.Name,
                description = link.Description,
                url = link.Url,
                icon = link.Icon
            });
        }

        public class QuickUpdateLinkRequest
        {
            public int LinkId { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Url { get; set; }
            public string? Icon { get; set; }
        }

        // POST: /Links/QuickUpdate  (Admin only, Edit Mode only -- edits an
        // existing link's fields from the live page, via the edit modal.)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdate([FromBody] QuickUpdateLinkRequest req)
        {
            if (req == null || req.LinkId <= 0 || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Url))
            {
                return BadRequest(new { success = false, message = "Link name and URL are both required." });
            }

            var link = await _context.Links.FindAsync(req.LinkId);
            if (link == null)
            {
                return NotFound(new { success = false, message = "Link not found." });
            }

            link.Name = req.Name.Trim();
            link.Description = string.IsNullOrWhiteSpace(req.Description) ? "" : req.Description.Trim();
            link.Url = req.Url.Trim();
            link.Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-link" : req.Icon.Trim();
            link.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                linkId = link.LinkId,
                name = link.Name,
                description = link.Description,
                url = link.Url,
                icon = link.Icon
            });
        }

        public class QuickDeleteLinkRequest
        {
            public int LinkId { get; set; }
        }

        // POST: /Links/QuickDelete  (Admin only -- lets the live trash button
        // remove a Link row without leaving the page.)
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickDelete([FromBody] QuickDeleteLinkRequest req)
        {
            if (req == null || req.LinkId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid link." });
            }

            var link = await _context.Links.FindAsync(req.LinkId);
            if (link == null)
            {
                return NotFound(new { success = false, message = "Link not found (it may already be deleted)." });
            }

            _context.Links.Remove(link);
            await _context.SaveChangesAsync();

            return Json(new { success = true, linkId = req.LinkId });
        }
    }
}

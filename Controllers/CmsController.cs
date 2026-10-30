using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    // Super Admins and Staff with authorized permissions can access CMS endpoints
    [Authorize(Roles = "Admin,Staff")]
    public class CmsController : Controller
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;

        // Allowed image types + a sane size limit (5 MB) to keep uploads safe.
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public CmsController(
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

        // POST: /Cms/SaveHomeContent
        // Saves the edited homepage HTML (called by the inline editor's "Save" button).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveHomeContent([FromForm] string html)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.HasPermissionAsync(user.Id, AppPermissions.ContentHomepage))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == HomeController.HomeSlug);
            if (page == null)
            {
                return NotFound(new { success = false, message = "Homepage record not found." });
            }

            page.ContentHtml = html;
            page.UpdatedAt = DateTime.UtcNow;
            page.UpdatedByUserId = user?.Id;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Homepage updated successfully." });
        }

        // POST: /Cms/SaveAboutHistoryCaaContent
        // Saves the edited "History of CAA in Pakistan" section on the About page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAboutHistoryCaaContent([FromForm] string html)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.HasPermissionAsync(user.Id, AppPermissions.ContentAbout))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == AboutController.HistoryCaaSlug);
                if (page == null)
                {
                    page = new Page
                    {
                        Slug = AboutController.HistoryCaaSlug,
                        Title = "About - History of CAA in Pakistan",
                        IsPublished = true
                    };
                    _context.Pages.Add(page);
                }

                page.ContentHtml = html;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Section updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Could not save — please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving. Nothing was changed." });
            }
        }

        // POST: /Cms/SaveAboutHistoryCatiContent
        // Saves the edited "History of CATI" section on the About page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAboutHistoryCatiContent([FromForm] string html)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.HasPermissionAsync(user.Id, AppPermissions.ContentAbout))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == AboutController.HistoryCatiSlug);
                if (page == null)
                {
                    page = new Page
                    {
                        Slug = AboutController.HistoryCatiSlug,
                        Title = "About - History of CATI",
                        IsPublished = true
                    };
                    _context.Pages.Add(page);
                }

                page.ContentHtml = html;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Section updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Could not save — please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving. Nothing was changed." });
            }
        }

        // POST: /Cms/SaveAboutManagementContent
        // Saves the edited "Management & Governance" section on the About page.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAboutManagementContent([FromForm] string html)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.HasPermissionAsync(user.Id, AppPermissions.ContentAbout))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == AboutController.ManagementSlug);
                if (page == null)
                {
                    page = new Page
                    {
                        Slug = AboutController.ManagementSlug,
                        Title = "About - Management & Governance",
                        IsPublished = true
                    };
                    _context.Pages.Add(page);
                }

                page.ContentHtml = html;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Section updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Could not save — please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving. Nothing was changed." });
            }
        }

        // POST: /Cms/SaveFinancialContent
        // Saves the edited Financial page content (Fee Structure, Scholarships, Payment Methods).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFinancialContent([FromForm] string html)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.HasPermissionAsync(user.Id, AppPermissions.ContentFinancial))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == FinancialController.FinancialSlug);
                if (page == null)
                {
                    page = new Page
                    {
                        Slug = FinancialController.FinancialSlug,
                        Title = "Financial Information - National Aviation University",
                        IsPublished = true
                    };
                    _context.Pages.Add(page);
                }

                page.ContentHtml = html;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Financial page updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Could not save — please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving. Nothing was changed." });
            }
        }

        // POST: /Cms/SavePageContent
        // Saves any database-backed page by slug (e.g. facilities, facility-library, etc.).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePageContent([FromForm] string slug, [FromForm] string html)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return BadRequest(new { success = false, message = "Page slug is required." });
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                return BadRequest(new { success = false, message = "Content cannot be empty." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || !await _permissionService.CanEditPageAsync(user.Id, slug))
            {
                return Forbid();
            }

            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == slug);
                if (page == null)
                {
                    page = new Page
                    {
                        Slug = slug,
                        Title = slug.Replace("-", " ").ToUpper(),
                        IsPublished = true
                    };
                    _context.Pages.Add(page);
                }

                page.ContentHtml = html;
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Page updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { success = false, message = "Could not save — please try again." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving. Nothing was changed." });
            }
        }

        // POST: /Cms/SaveFacilityContent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFacilityContent([FromForm] string slug, [FromForm] string html)
        {
            return await SavePageContent(slug, html);
        }

        // POST: /Cms/UploadImage
        // Accepts a single image file, stores it under wwwroot/uploads, records it in MediaLibrary,
        // and returns the public URL so the front-end editor can swap the <img src="..."> immediately.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSizeBytes)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file received." });
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest(new { success = false, message = "File is too large. Max size is 5 MB." });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Unsupported file type." });
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/{uniqueName}";
            var user = await _userManager.GetUserAsync(User);

            _context.MediaLibraries.Add(new MediaLibrary
            {
                FileName = file.FileName ?? "file",
                FilePath = publicUrl,
                FileType = file.ContentType,
                FileSizeBytes = file.Length,
                UploadedByUserId = user?.Id,
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, url = publicUrl });
        }

        // POST: /Cms/UploadVideo
        // Accepts a single video file (used to replace the hero slideshow's
        // video slide), stores it under wwwroot/uploads, records it in
        // MediaLibrary, and returns the public URL.
        private static readonly string[] AllowedVideoExtensions = { ".mp4", ".webm", ".ogg", ".mov", ".m4v", ".avi", ".mkv", ".wmv", ".3gp", ".flv" };
        private const long MaxVideoSizeBytes = 300 * 1024 * 1024; // 300 MB - room for a full-quality hero video

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxVideoSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoSizeBytes)]
        public async Task<IActionResult> UploadVideo(IFormFile file)
        {
            var maxRequestBodySizeFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (maxRequestBodySizeFeature != null && !maxRequestBodySizeFeature.IsReadOnly)
            {
                maxRequestBodySizeFeature.MaxRequestBodySize = MaxVideoSizeBytes;
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { success = false, message = "No file received." });
            }

            if (file.Length > MaxVideoSizeBytes)
            {
                return BadRequest(new { success = false, message = "File is too large. Max size is 300 MB." });
            }

            var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedVideoExtensions.Contains(ext))
            {
                return BadRequest(new { success = false, message = "Unsupported file type. Use MP4, WebM, MOV, M4V, OGG, or AVI." });
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, uniqueName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/{uniqueName}";
            var user = await _userManager.GetUserAsync(User);

            _context.MediaLibraries.Add(new MediaLibrary
            {
                FileName = file.FileName ?? "video",
                FilePath = publicUrl,
                FileType = file.ContentType,
                FileSizeBytes = file.Length,
                UploadedByUserId = user?.Id,
                UploadedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true, url = publicUrl });
        }
    }
}

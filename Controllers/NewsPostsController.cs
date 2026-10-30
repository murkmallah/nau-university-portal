using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;
using System.Globalization;

[Authorize(Roles = "Admin,Staff")]
public class NewsPostsController : Controller
{
    private readonly NauDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPermissionService _permissionService;

    public NewsPostsController(
        NauDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    // ================= PUBLIC-FACING PAGE (no login required) =================
    // GET: /NewsPosts/Public -> "Latest News & Announcements" section
    [AllowAnonymous]
    public async Task<IActionResult> Public(string? edit = null)
    {
        bool wantsEdit = !string.IsNullOrEmpty(edit) &&
            (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

        ApplicationUser? currentUser = null;
        bool hasNewsPerm = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                hasNewsPerm = await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentNewsAndMedia)
                           || await _permissionService.IsSuperAdminAsync(currentUser.Id);
            }
        }

        bool isEditMode = wantsEdit && currentUser != null && (User.IsInRole("Admin") || hasNewsPerm);
        ViewBag.EditMode = isEditMode;
        ViewBag.IsAdmin = currentUser != null && (User.IsInRole("Admin") || hasNewsPerm);

        ViewBag.CurrentUserName = "";
        if (currentUser != null)
        {
            ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (User.Identity?.Name ?? "");
        }

        bool isAdminUser = currentUser != null && (User.IsInRole("Admin") || User.IsInRole("Staff") || hasNewsPerm);
        ViewBag.IsAdminUser = isAdminUser;

        IQueryable<NewsPost> postsQuery = _context.NewsPosts;
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (!isAdminUser)
        {
            // Public visitors (not logged in as Admin/Staff) only see active posts that have not expired
            postsQuery = postsQuery.Where(n => n.IsActive && (n.ExpiresAt == null || n.ExpiresAt >= today));
        }

        var posts = await postsQuery
            .OrderByDescending(n => n.PublishDate)
            .ThenByDescending(n => n.NewsPostId)
            .ToListAsync();

        // Photo Gallery section -- separate table, own admin controls, does
        // not affect the News list above in any way.
        ViewBag.GalleryPhotos = await _context.GalleryPhotos
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.PhotoId)
            .ToListAsync();

        // Video Gallery section -- separate table, own admin controls, shown
        // as the 3rd section of this same page (no longer a standalone page).
        ViewBag.GalleryVideos = await _context.GalleryVideos
            .OrderBy(v => v.DisplayOrder)
            .ThenByDescending(v => v.PublishDate)
            .ThenByDescending(v => v.VideoId)
            .ToListAsync();

        return View(posts);
    }

    // GET: /NewsPosts/GetActiveSplashNews (Public endpoint for splash popup banner)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveSplashNews()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var post = await _context.NewsPosts
            .Where(n => n.IsActive && n.IsSplashScreen && (n.ExpiresAt == null || n.ExpiresAt >= today))
            .OrderByDescending(n => n.PublishDate)
            .ThenByDescending(n => n.NewsPostId)
            .FirstOrDefaultAsync();

        if (post == null)
        {
            return Json(new { hasSplash = false });
        }

        var style = GetCategoryStyle(post.Category);
        return Json(new
        {
            hasSplash = true,
            newsPostId = post.NewsPostId,
            title = post.Title,
            body = post.Body,
            category = post.Category ?? "Announcement",
            icon = string.IsNullOrWhiteSpace(post.Icon) ? style.Icon : post.Icon,
            color = style.Color,
            imageUrl = post.ImageUrl,
            publishDate = post.PublishDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)
        });
    }

    private static (string Color, string Icon) GetCategoryStyle(string? category)
    {
        var c = category ?? string.Empty;

        if (c.Contains("cpd", StringComparison.OrdinalIgnoreCase) || c.Contains("seminar", StringComparison.OrdinalIgnoreCase))
        {
            return ("#1a5fa6", "fas fa-chalkboard-teacher");
        }
        if (c.Contains("admission", StringComparison.OrdinalIgnoreCase))
        {
            return ("#c87800", "fas fa-graduation-cap");
        }
        if (c.Contains("academic", StringComparison.OrdinalIgnoreCase))
        {
            return ("#7c3aed", "fas fa-award");
        }
        if (c.Contains("event", StringComparison.OrdinalIgnoreCase) || c.Contains("training", StringComparison.OrdinalIgnoreCase))
        {
            return ("#1e8e3e", "fas fa-calendar-check");
        }

        return ("#0b2c4d", "fas fa-newspaper");
    }

    public class QuickAddNewsRequest
    {
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
        public string? ImageUrl { get; set; }
        public string? PublishDate { get; set; } // "yyyy-MM-dd"
        public string? ExpiresAt { get; set; }   // "yyyy-MM-dd"
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSplashScreen { get; set; } = false;
    }

    // POST: /NewsPosts/QuickAdd  (Admin only, Edit Mode only -- lets the live
    // "+ Add News" button on the public page create a real NewsPost row
    // without leaving the page.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdd([FromBody] QuickAddNewsRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "News title is required." });
        }

        var publishDate = TryParseDate(req.PublishDate) ?? DateOnly.FromDateTime(DateTime.Now);
        var expiresAt = TryParseDate(req.ExpiresAt);
        var currentUser = await _userManager.GetUserAsync(User);

        if (req.IsSplashScreen)
        {
            var otherSplashes = await _context.NewsPosts.Where(n => n.IsSplashScreen).ToListAsync();
            foreach (var sp in otherSplashes)
            {
                sp.IsSplashScreen = false;
            }
        }

        var post = new NewsPost
        {
            Title = req.Title.Trim(),
            Body = string.IsNullOrWhiteSpace(req.Body) ? null : req.Body.Trim(),
            Category = string.IsNullOrWhiteSpace(req.Category) ? "News" : req.Category.Trim(),
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim(),
            PublishDate = publishDate,
            ExpiresAt = expiresAt,
            IsFeatured = req.IsFeatured,
            IsActive = req.IsActive,
            IsSplashScreen = req.IsSplashScreen,
            CreatedByUserId = currentUser?.Id,
            CreatedAt = DateTime.Now
        };

        _context.NewsPosts.Add(post);
        await _context.SaveChangesAsync();

        var style = GetCategoryStyle(post.Category);

        return Json(new
        {
            success = true,
            newsPostId = post.NewsPostId,
            title = post.Title,
            body = post.Body,
            category = post.Category,
            icon = string.IsNullOrWhiteSpace(post.Icon) ? style.Icon : post.Icon,
            color = style.Color,
            imageUrl = post.ImageUrl,
            publishDate = post.PublishDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            publishDateRaw = post.PublishDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            expiresAt = post.ExpiresAt?.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            expiresAtRaw = post.ExpiresAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            isFeatured = post.IsFeatured,
            isActive = post.IsActive,
            isSplashScreen = post.IsSplashScreen
        });
    }

    public class QuickUpdateNewsRequest
    {
        public int NewsPostId { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Category { get; set; }
        public string? Icon { get; set; }
        public string? ImageUrl { get; set; }
        public string? PublishDate { get; set; }
        public string? ExpiresAt { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSplashScreen { get; set; } = false;
    }

    // POST: /NewsPosts/QuickUpdate  (Admin only, Edit Mode only -- edits an
    // existing news post's fields from the live page, via the edit modal.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdate([FromBody] QuickUpdateNewsRequest req)
    {
        if (req == null || req.NewsPostId <= 0 || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "News title is required." });
        }

        var post = await _context.NewsPosts.FindAsync(req.NewsPostId);
        if (post == null)
        {
            return NotFound(new { success = false, message = "News post not found." });
        }

        if (req.IsSplashScreen && !post.IsSplashScreen)
        {
            var otherSplashes = await _context.NewsPosts.Where(n => n.NewsPostId != post.NewsPostId && n.IsSplashScreen).ToListAsync();
            foreach (var sp in otherSplashes)
            {
                sp.IsSplashScreen = false;
            }
        }

        post.Title = req.Title.Trim();
        post.Body = string.IsNullOrWhiteSpace(req.Body) ? null : req.Body.Trim();
        post.Category = string.IsNullOrWhiteSpace(req.Category) ? "News" : req.Category.Trim();
        post.Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim();
        post.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim();
        post.PublishDate = TryParseDate(req.PublishDate) ?? post.PublishDate;
        post.ExpiresAt = TryParseDate(req.ExpiresAt);
        post.IsFeatured = req.IsFeatured;
        post.IsActive = req.IsActive;
        post.IsSplashScreen = req.IsSplashScreen;

        await _context.SaveChangesAsync();

        var style = GetCategoryStyle(post.Category);

        return Json(new
        {
            success = true,
            newsPostId = post.NewsPostId,
            title = post.Title,
            body = post.Body,
            category = post.Category,
            icon = string.IsNullOrWhiteSpace(post.Icon) ? style.Icon : post.Icon,
            color = style.Color,
            imageUrl = post.ImageUrl,
            publishDate = post.PublishDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            publishDateRaw = post.PublishDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            expiresAt = post.ExpiresAt?.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
            expiresAtRaw = post.ExpiresAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            isFeatured = post.IsFeatured,
            isActive = post.IsActive,
            isSplashScreen = post.IsSplashScreen
        });
    }

    public class ToggleActiveNewsRequest
    {
        public int NewsPostId { get; set; }
    }

    // POST: /NewsPosts/ToggleActive  (Admin/Staff -- toggles IsActive status of a news post)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive([FromBody] ToggleActiveNewsRequest req)
    {
        if (req == null || req.NewsPostId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid news post ID." });
        }

        var post = await _context.NewsPosts.FindAsync(req.NewsPostId);
        if (post == null)
        {
            return NotFound(new { success = false, message = "News post not found." });
        }

        post.IsActive = !post.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, newsPostId = post.NewsPostId, isActive = post.IsActive });
    }

    public class QuickDeleteNewsRequest
    {
        public int NewsPostId { get; set; }
    }

    // POST: /NewsPosts/QuickDelete  (Admin only -- lets the live "🗑" button
    // on the public page remove a NewsPost row without leaving the page)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDelete([FromBody] QuickDeleteNewsRequest req)
    {
        if (req == null || req.NewsPostId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid news post." });
        }

        var post = await _context.NewsPosts.FindAsync(req.NewsPostId);
        if (post == null)
        {
            return NotFound(new { success = false, message = "News post not found (it may already be deleted)." });
        }

        _context.NewsPosts.Remove(post);
        await _context.SaveChangesAsync();

        return Json(new { success = true, newsPostId = req.NewsPostId });
    }

    // POST: /NewsPosts/UploadNewsImage (Admin only -- accepts an image file for a news post banner)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<IActionResult> UploadNewsImage(IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file received." });
        }

        if (file.Length > MaxPhotoSizeBytes)
        {
            return BadRequest(new { success = false, message = "File is too large. Max size is 5 MB." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(ext))
        {
            return BadRequest(new { success = false, message = "Unsupported file type. Use JPG, PNG, GIF, or WEBP." });
        }

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
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
            FileName = file.FileName,
            FilePath = publicUrl,
            FileType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = user?.Id,
            UploadedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Json(new { success = true, url = publicUrl });
    }

    // ================= PHOTO GALLERY (admin-only quick actions) =================
    // Everything below manages GalleryPhoto rows for the "Photo Gallery" section
    // on the same /NewsPosts/Public page. Kept separate from the News post
    // actions above so nothing here can ever touch a NewsPost row.

    private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024; // 5 MB

    public class QuickAddPhotoRequest
    {
        public string? Caption { get; set; }
        public string? Album { get; set; }
        public string? ImageUrl { get; set; }
        public string? Icon { get; set; }
    }

    // POST: /NewsPosts/QuickAddPhoto  (Admin only, Edit Mode only -- lets the
    // live "+ Add Photo" button create a real GalleryPhoto row without leaving
    // the page.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddPhoto([FromBody] QuickAddPhotoRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Caption))
        {
            return BadRequest(new { success = false, message = "Photo caption is required." });
        }

        var currentUser = await _userManager.GetUserAsync(User);

        int nextOrder = (await _context.GalleryPhotos.AnyAsync())
            ? await _context.GalleryPhotos.MaxAsync(g => g.DisplayOrder) + 1
            : 0;

        var photo = new GalleryPhoto
        {
            Caption = req.Caption.Trim(),
            Album = string.IsNullOrWhiteSpace(req.Album) ? "Campus Life" : req.Album.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim(),
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-image" : req.Icon.Trim(),
            DisplayOrder = nextOrder,
            CreatedByUserId = currentUser?.Id,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.GalleryPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            photoId = photo.PhotoId,
            caption = photo.Caption,
            album = photo.Album,
            imageUrl = photo.ImageUrl,
            icon = photo.Icon
        });
    }

    public class QuickUpdatePhotoRequest
    {
        public int PhotoId { get; set; }
        public string? Caption { get; set; }
        public string? Album { get; set; }
        public string? ImageUrl { get; set; }
        public string? Icon { get; set; }
    }

    // POST: /NewsPosts/QuickUpdatePhoto  (Admin only, Edit Mode only -- edits
    // an existing photo's caption/album/image from the live page.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdatePhoto([FromBody] QuickUpdatePhotoRequest req)
    {
        if (req == null || req.PhotoId <= 0 || string.IsNullOrWhiteSpace(req.Caption))
        {
            return BadRequest(new { success = false, message = "Photo caption is required." });
        }

        var photo = await _context.GalleryPhotos.FindAsync(req.PhotoId);
        if (photo == null)
        {
            return NotFound(new { success = false, message = "Photo not found." });
        }

        photo.Caption = req.Caption.Trim();
        photo.Album = string.IsNullOrWhiteSpace(req.Album) ? "Campus Life" : req.Album.Trim();
        photo.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim();
        photo.Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-image" : req.Icon.Trim();
        photo.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            photoId = photo.PhotoId,
            caption = photo.Caption,
            album = photo.Album,
            imageUrl = photo.ImageUrl,
            icon = photo.Icon
        });
    }

    public class QuickDeletePhotoRequest
    {
        public int PhotoId { get; set; }
    }

    // POST: /NewsPosts/QuickDeletePhoto  (Admin only -- lets the live "trash"
    // button on a photo tile remove a GalleryPhoto row without leaving the page)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeletePhoto([FromBody] QuickDeletePhotoRequest req)
    {
        if (req == null || req.PhotoId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid photo." });
        }

        var photo = await _context.GalleryPhotos.FindAsync(req.PhotoId);
        if (photo == null)
        {
            return NotFound(new { success = false, message = "Photo not found (it may already be deleted)." });
        }

        _context.GalleryPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        return Json(new { success = true, photoId = req.PhotoId });
    }

    // POST: /NewsPosts/UploadGalleryPhoto  (Admin only -- accepts a real image
    // file from the "Add Photo" / "Edit Photo" form, stores it under
    // wwwroot/uploads, records it in MediaLibrary -- same convention as
    // /Cms/UploadImage -- and returns the public URL to store on the
    // GalleryPhoto row.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxPhotoSizeBytes)]
    public async Task<IActionResult> UploadGalleryPhoto(IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file received." });
        }

        if (file.Length > MaxPhotoSizeBytes)
        {
            return BadRequest(new { success = false, message = "File is too large. Max size is 5 MB." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedPhotoExtensions.Contains(ext))
        {
            return BadRequest(new { success = false, message = "Unsupported file type. Use JPG, PNG, GIF, or WEBP." });
        }

        var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
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
            FileName = file.FileName,
            FilePath = publicUrl,
            FileType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedByUserId = user?.Id,
            UploadedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Json(new { success = true, url = publicUrl });
    }

    // =============================================================================

    private static DateOnly? TryParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        if (DateOnly.TryParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
        {
            return d;
        }
        if (DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }
        return null;
    }

    // =============================================================================

    // GET: NEWSPOSTS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.NewsPosts.ToListAsync());
    }

    // GET: NEWSPOSTS/Details/5
    public async Task<IActionResult> Details(int? newspostid)
    {
        if (newspostid == null)
        {
            return NotFound();
        }

        var newspost = await _context.NewsPosts
            .FirstOrDefaultAsync(m => m.NewsPostId == newspostid);
        if (newspost == null)
        {
            return NotFound();
        }

        return View(newspost);
    }

    // GET: NEWSPOSTS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NEWSPOSTS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NewsPostId,Title,Body,Category,Icon,ImageUrl,PublishDate,IsFeatured,IsActive,IsSplashScreen,CreatedByUserId,CreatedAt,CreatedByUser")] NewsPost newspost)
    {
        if (ModelState.IsValid)
        {
            _context.Add(newspost);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(newspost);
    }

    // GET: NEWSPOSTS/Edit/5
    public async Task<IActionResult> Edit(int? newspostid)
    {
        if (newspostid == null)
        {
            return NotFound();
        }

        var newspost = await _context.NewsPosts.FindAsync(newspostid);
        if (newspost == null)
        {
            return NotFound();
        }
        return View(newspost);
    }

    // POST: NEWSPOSTS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? newspostid, [Bind("NewsPostId,Title,Body,Category,Icon,ImageUrl,PublishDate,IsFeatured,IsActive,IsSplashScreen,CreatedByUserId,CreatedAt,CreatedByUser")] NewsPost newspost)
    {
        if (newspostid != newspost.NewsPostId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(newspost);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NewsPostExists(newspost.NewsPostId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(newspost);
    }

    // GET: NEWSPOSTS/Delete/5
    public async Task<IActionResult> Delete(int? newspostid)
    {
        if (newspostid == null)
        {
            return NotFound();
        }

        var newspost = await _context.NewsPosts
            .FirstOrDefaultAsync(m => m.NewsPostId == newspostid);
        if (newspost == null)
        {
            return NotFound();
        }

        return View(newspost);
    }

    // POST: NEWSPOSTS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? newspostid)
    {
        var newspost = await _context.NewsPosts.FindAsync(newspostid);
        if (newspost != null)
        {
            _context.NewsPosts.Remove(newspost);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool NewsPostExists(int? newspostid)
    {
        return _context.NewsPosts.Any(e => e.NewsPostId == newspostid);
    }
}

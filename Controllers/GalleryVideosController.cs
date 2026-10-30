using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using System.Globalization;

[Authorize(Roles = "Admin")]
public class GalleryVideosController : Controller
{
    private readonly NauDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GalleryVideosController(NauDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // NOTE: Video Gallery no longer has its own public page/route. It is now
    // rendered as the 3rd section (after Photo Gallery) on the same
    // News & Media page -- see NewsPostsController.Public() and
    // Views/NewsPosts/Public.cshtml. This controller only exposes the
    // QuickAdd/QuickUpdate/QuickDelete AJAX endpoints used by that page's
    // inline admin edit controls.

    private static string GetCategoryColor(string? category)
    {
        var c = category ?? string.Empty;

        if (c.Contains("training", StringComparison.OrdinalIgnoreCase))
            return "#1e8e3e";
        if (c.Contains("cpd", StringComparison.OrdinalIgnoreCase))
            return "#1e8e3e";
        if (c.Contains("ceremony", StringComparison.OrdinalIgnoreCase) || c.Contains("convocation", StringComparison.OrdinalIgnoreCase))
            return "#7c3aed";
        if (c.Contains("campus life", StringComparison.OrdinalIgnoreCase) || c.Contains("sport", StringComparison.OrdinalIgnoreCase))
            return "#c87800";
        if (c.Contains("tour", StringComparison.OrdinalIgnoreCase))
            return "#1a5fa6";

        return "#0b2c4d";
    }

    public class QuickAddVideoRequest
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Icon { get; set; }
        public string? Duration { get; set; }
        public int Views { get; set; }
        public string? PublishDate { get; set; } // "yyyy-MM-dd"
    }

    // POST: /GalleryVideos/QuickAdd  (Admin only, Edit Mode only -- lets the
    // live "+ Add Video" button create a real GalleryVideo row without
    // leaving the page.)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdd([FromBody] QuickAddVideoRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.VideoUrl))
        {
            return BadRequest(new { success = false, message = "Video title and video link are both required." });
        }

        var publishDate = TryParseDate(req.PublishDate) ?? DateOnly.FromDateTime(DateTime.Now);
        var currentUser = await _userManager.GetUserAsync(User);

        var maxOrder = await _context.GalleryVideos.Select(v => (int?)v.DisplayOrder).MaxAsync() ?? 0;

        var video = new GalleryVideo
        {
            Title = req.Title.Trim(),
            Category = string.IsNullOrWhiteSpace(req.Category) ? "Campus Life" : req.Category.Trim(),
            VideoUrl = req.VideoUrl.Trim(),
            ThumbnailUrl = string.IsNullOrWhiteSpace(req.ThumbnailUrl) ? null : req.ThumbnailUrl.Trim(),
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim(),
            Duration = string.IsNullOrWhiteSpace(req.Duration) ? null : req.Duration.Trim(),
            Views = req.Views < 0 ? 0 : req.Views,
            PublishDate = publishDate,
            DisplayOrder = maxOrder + 1,
            CreatedByUserId = currentUser?.Id,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.GalleryVideos.Add(video);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            videoId = video.VideoId,
            title = video.Title,
            category = video.Category,
            color = GetCategoryColor(video.Category),
            videoUrl = video.VideoUrl,
            thumbnailUrl = video.ThumbnailUrl,
            icon = string.IsNullOrWhiteSpace(video.Icon) ? "fas fa-film" : video.Icon,
            duration = video.Duration,
            views = video.Views,
            publishDate = video.PublishDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
            publishDateRaw = video.PublishDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });
    }

    public class QuickUpdateVideoRequest
    {
        public int VideoId { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? VideoUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Icon { get; set; }
        public string? Duration { get; set; }
        public int Views { get; set; }
        public string? PublishDate { get; set; }
    }

    // POST: /GalleryVideos/QuickUpdate  (Admin only, Edit Mode only -- edits
    // an existing video's fields from the live page, via the edit modal.)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdate([FromBody] QuickUpdateVideoRequest req)
    {
        if (req == null || req.VideoId <= 0 || string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.VideoUrl))
        {
            return BadRequest(new { success = false, message = "Video title and video link are both required." });
        }

        var video = await _context.GalleryVideos.FindAsync(req.VideoId);
        if (video == null)
        {
            return NotFound(new { success = false, message = "Video not found." });
        }

        video.Title = req.Title.Trim();
        video.Category = string.IsNullOrWhiteSpace(req.Category) ? "Campus Life" : req.Category.Trim();
        video.VideoUrl = req.VideoUrl.Trim();
        video.ThumbnailUrl = string.IsNullOrWhiteSpace(req.ThumbnailUrl) ? null : req.ThumbnailUrl.Trim();
        video.Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim();
        video.Duration = string.IsNullOrWhiteSpace(req.Duration) ? null : req.Duration.Trim();
        video.Views = req.Views < 0 ? 0 : req.Views;
        video.PublishDate = TryParseDate(req.PublishDate) ?? video.PublishDate;
        video.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            videoId = video.VideoId,
            title = video.Title,
            category = video.Category,
            color = GetCategoryColor(video.Category),
            videoUrl = video.VideoUrl,
            thumbnailUrl = video.ThumbnailUrl,
            icon = string.IsNullOrWhiteSpace(video.Icon) ? "fas fa-film" : video.Icon,
            duration = video.Duration,
            views = video.Views,
            publishDate = video.PublishDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
            publishDateRaw = video.PublishDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        });
    }

    public class QuickDeleteVideoRequest
    {
        public int VideoId { get; set; }
    }

    // POST: /GalleryVideos/QuickDelete  (Admin only -- lets the live trash
    // button remove a GalleryVideo row without leaving the page.)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDelete([FromBody] QuickDeleteVideoRequest req)
    {
        if (req == null || req.VideoId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid video." });
        }

        var video = await _context.GalleryVideos.FindAsync(req.VideoId);
        if (video == null)
        {
            return NotFound(new { success = false, message = "Video not found (it may already be deleted)." });
        }

        _context.GalleryVideos.Remove(video);
        await _context.SaveChangesAsync();

        return Json(new { success = true, videoId = req.VideoId });
    }

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
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using System.Text.Json;

namespace NAU_Web.Controllers
{
    public class CourseCatalogController : Controller
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public const string CatalogSlug = "course-catalog";

        public CourseCatalogController(NauDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
        }

        // GET: /CourseCatalog or /course-catalog.html
        [AllowAnonymous]
        [HttpGet("/CourseCatalog")]
        [HttpGet("/course-catalog.html")]
        [HttpGet("/CourseCatalog/Index")]
        public async Task<IActionResult> Index(string? edit = null)
        {
            bool wantsEdit = !string.IsNullOrEmpty(edit) &&
                (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

            ViewBag.EditMode = wantsEdit && User.Identity != null && User.Identity.IsAuthenticated && User.IsInRole("Admin");

            ViewBag.CurrentUserName = "";
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewBag.CurrentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity.Name ?? "");
            }

            var content = await LoadOrSeedCatalogContentAsync();
            return View(content);
        }

        private async Task<CourseCatalogPageContent> LoadOrSeedCatalogContentAsync()
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == CatalogSlug);
            CourseCatalogPageContent? data = null;

            if (page != null && !string.IsNullOrWhiteSpace(page.ContentHtml))
            {
                try
                {
                    data = JsonSerializer.Deserialize<CourseCatalogPageContent>(page.ContentHtml);
                }
                catch
                {
                    data = null;
                }
            }

            if (data == null || data.Schools == null || !data.Schools.Any())
            {
                data = await GetDefaultCatalogDataAsync();

                if (page == null)
                {
                    page = new Page
                    {
                        Slug = CatalogSlug,
                        Title = "Course Catalog - National Aviation University",
                        ContentHtml = JsonSerializer.Serialize(data),
                        IsPublished = true,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Pages.Add(page);
                }
                else
                {
                    page.ContentHtml = JsonSerializer.Serialize(data);
                    page.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            // Auto-sync courses added via Schools & Courses (_context.Schools & _context.Courses)
            if (data != null && data.Schools != null)
            {
                var dbSchools = await _context.Schools.Include(s => s.Courses).ToListAsync();
                bool changed = false;

                foreach (var dbSchool in dbSchools)
                {
                    var catSchool = data.Schools.FirstOrDefault(s =>
                        s.Name.Equals(dbSchool.Name, StringComparison.OrdinalIgnoreCase) ||
                        (s.Id > 0 && s.Id == dbSchool.SchoolId));

                    if (catSchool == null)
                    {
                        catSchool = new CourseCatalogSchool
                        {
                            Id = dbSchool.SchoolId,
                            Name = dbSchool.Name,
                            Slug = !string.IsNullOrWhiteSpace(dbSchool.Name) ? dbSchool.Name.ToLowerInvariant().Replace(" ", "-") : "school",
                            Icon = !string.IsNullOrWhiteSpace(dbSchool.Icon) ? dbSchool.Icon : "fa-graduation-cap",
                            Color = !string.IsNullOrWhiteSpace(dbSchool.ColorHex) ? dbSchool.ColorHex : "#0b2c4d",
                            Courses = new List<CourseCatalogItem>()
                        };
                        data.Schools.Add(catSchool);
                        changed = true;
                    }

                    if (dbSchool.Courses != null)
                    {
                        foreach (var dbCourse in dbSchool.Courses)
                        {
                            var exists = catSchool.Courses.Any(c =>
                                c.Title.Equals(dbCourse.Title, StringComparison.OrdinalIgnoreCase) ||
                                (c.Id > 0 && c.Id == dbCourse.CourseId));

                            if (!exists)
                            {
                                int nextCourseId = 1;
                                foreach (var s in data.Schools)
                                {
                                    foreach (var c in s.Courses)
                                    {
                                        if (c.Id >= nextCourseId) nextCourseId = c.Id + 1;
                                    }
                                }

                                string startStr = dbCourse.IsTbd || dbCourse.StartDate == null ? "TBD" : dbCourse.StartDate.Value.ToString("MMM yyyy");
                                string endStr = dbCourse.IsTbd || dbCourse.EndDate == null ? "TBD" : dbCourse.EndDate.Value.ToString("MMM yyyy");

                                catSchool.Courses.Add(new CourseCatalogItem
                                {
                                    Id = nextCourseId,
                                    Title = dbCourse.Title,
                                    Duration = !string.IsNullOrWhiteSpace(dbCourse.Duration) ? dbCourse.Duration : "01 Week",
                                    StartDate = startStr,
                                    EndDate = endStr,
                                    Note = "Part of NAU – ATP 2026 Academic Training Plan"
                                });
                                changed = true;
                            }
                        }
                    }
                }

                if (changed && page != null)
                {
                    page.ContentHtml = JsonSerializer.Serialize(data);
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return data ?? new CourseCatalogPageContent();
        }

        private async Task<CourseCatalogPageContent> GetDefaultCatalogDataAsync()
        {
            var seedPath = Path.Combine(_env.ContentRootPath, "SeedData", "course-catalog-content.json");
            if (System.IO.File.Exists(seedPath))
            {
                try
                {
                    var json = await System.IO.File.ReadAllTextAsync(seedPath);
                    var deserialized = JsonSerializer.Deserialize<CourseCatalogPageContent>(json);
                    if (deserialized != null && deserialized.Schools != null && deserialized.Schools.Any())
                    {
                        return deserialized;
                    }
                }
                catch
                {
                    // Fallback to default
                }
            }

            return new CourseCatalogPageContent();
        }

        private async Task SaveCatalogContentAsync(CourseCatalogPageContent data)
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == CatalogSlug);
            if (page == null)
            {
                page = new Page
                {
                    Slug = CatalogSlug,
                    Title = "Course Catalog - National Aviation University",
                    ContentHtml = JsonSerializer.Serialize(data),
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Pages.Add(page);
            }
            else
            {
                page.ContentHtml = JsonSerializer.Serialize(data);
                page.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        // ================= ADMIN AJAX ENDPOINTS =================

        public class QuickSaveHeroRequest
        {
            public string? Eyebrow { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSaveHero([FromBody] QuickSaveHeroRequest req)
        {
            if (req == null) return BadRequest(new { success = false, message = "Invalid request." });

            var content = await LoadOrSeedCatalogContentAsync();
            content.Hero.Eyebrow = req.Eyebrow?.Trim() ?? "ATP 2026";
            content.Hero.Title = req.Title?.Trim() ?? "NAU Course Catalog";
            content.Hero.Description = req.Description?.Trim() ?? "";

            await SaveCatalogContentAsync(content);
            return Json(new { success = true, hero = content.Hero });
        }

        public class QuickAddCourseRequest
        {
            public int SchoolId { get; set; }
            public string? Title { get; set; }
            public string? Duration { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddCourse([FromBody] QuickAddCourseRequest req)
        {
            if (req == null || req.SchoolId <= 0 || string.IsNullOrWhiteSpace(req.Title))
            {
                return BadRequest(new { success = false, message = "Course title and school are required." });
            }

            var content = await LoadOrSeedCatalogContentAsync();
            var school = content.Schools.FirstOrDefault(s => s.Id == req.SchoolId);
            if (school == null)
            {
                return NotFound(new { success = false, message = "School not found." });
            }

            int nextId = 1;
            foreach (var s in content.Schools)
            {
                foreach (var c in s.Courses)
                {
                    if (c.Id >= nextId) nextId = c.Id + 1;
                }
            }

            var newCourse = new CourseCatalogItem
            {
                Id = nextId,
                Title = req.Title.Trim(),
                Duration = string.IsNullOrWhiteSpace(req.Duration) ? "01 Week" : req.Duration.Trim(),
                StartDate = string.IsNullOrWhiteSpace(req.StartDate) ? "TBD" : req.StartDate.Trim(),
                EndDate = string.IsNullOrWhiteSpace(req.EndDate) ? "TBD" : req.EndDate.Trim(),
                Note = string.IsNullOrWhiteSpace(req.Note) ? "Part of NAU – ATP 2026 Academic Training Plan, Hyderabad" : req.Note.Trim()
            };

            school.Courses.Add(newCourse);
            await SaveCatalogContentAsync(content);

            return Json(new { success = true, course = newCourse });
        }

        public class QuickUpdateCourseRequest
        {
            public int CourseId { get; set; }
            public int SchoolId { get; set; }
            public string? Title { get; set; }
            public string? Duration { get; set; }
            public string? StartDate { get; set; }
            public string? EndDate { get; set; }
            public string? Note { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickUpdateCourse([FromBody] QuickUpdateCourseRequest req)
        {
            if (req == null || req.CourseId <= 0 || string.IsNullOrWhiteSpace(req.Title))
            {
                return BadRequest(new { success = false, message = "Course title is required." });
            }

            var content = await LoadOrSeedCatalogContentAsync();
            CourseCatalogItem? foundCourse = null;
            CourseCatalogSchool? currentSchool = null;

            foreach (var s in content.Schools)
            {
                var c = s.Courses.FirstOrDefault(x => x.Id == req.CourseId);
                if (c != null)
                {
                    foundCourse = c;
                    currentSchool = s;
                    break;
                }
            }

            if (foundCourse == null || currentSchool == null)
            {
                return NotFound(new { success = false, message = "Course not found." });
            }

            foundCourse.Title = req.Title.Trim();
            foundCourse.Duration = string.IsNullOrWhiteSpace(req.Duration) ? "01 Week" : req.Duration.Trim();
            foundCourse.StartDate = string.IsNullOrWhiteSpace(req.StartDate) ? "TBD" : req.StartDate.Trim();
            foundCourse.EndDate = string.IsNullOrWhiteSpace(req.EndDate) ? "TBD" : req.EndDate.Trim();
            foundCourse.Note = string.IsNullOrWhiteSpace(req.Note) ? "Part of NAU – ATP 2026 Academic Training Plan, Hyderabad" : req.Note.Trim();

            // If school was changed
            if (req.SchoolId > 0 && req.SchoolId != currentSchool.Id)
            {
                var targetSchool = content.Schools.FirstOrDefault(s => s.Id == req.SchoolId);
                if (targetSchool != null)
                {
                    currentSchool.Courses.Remove(foundCourse);
                    targetSchool.Courses.Add(foundCourse);
                }
            }

            await SaveCatalogContentAsync(content);
            return Json(new { success = true, course = foundCourse });
        }

        public class QuickDeleteCourseRequest
        {
            public int CourseId { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickDeleteCourse([FromBody] QuickDeleteCourseRequest req)
        {
            if (req == null || req.CourseId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid course." });
            }

            var content = await LoadOrSeedCatalogContentAsync();
            bool removed = false;

            foreach (var s in content.Schools)
            {
                var c = s.Courses.FirstOrDefault(x => x.Id == req.CourseId);
                if (c != null)
                {
                    s.Courses.Remove(c);
                    removed = true;
                    break;
                }
            }

            if (!removed)
            {
                return NotFound(new { success = false, message = "Course not found (may already be deleted)." });
            }

            await SaveCatalogContentAsync(content);
            return Json(new { success = true, courseId = req.CourseId });
        }

        public class QuickAddSchoolRequest
        {
            public string? Name { get; set; }
            public string? Icon { get; set; }
            public string? Color { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddSchool([FromBody] QuickAddSchoolRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest(new { success = false, message = "School name is required." });
            }

            var content = await LoadOrSeedCatalogContentAsync();
            int nextId = content.Schools.Any() ? content.Schools.Max(s => s.Id) + 1 : 1;

            var newSchool = new CourseCatalogSchool
            {
                Id = nextId,
                Name = req.Name.Trim(),
                Slug = req.Name.Trim().ToLower().Replace(" ", "-"),
                Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fa-graduation-cap" : req.Icon.Trim(),
                Color = string.IsNullOrWhiteSpace(req.Color) ? "#0b2c4d" : req.Color.Trim(),
                Courses = new List<CourseCatalogItem>()
            };

            content.Schools.Add(newSchool);
            await SaveCatalogContentAsync(content);

            return Json(new { success = true, school = newSchool });
        }

        public class QuickDeleteSchoolRequest
        {
            public int SchoolId { get; set; }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickDeleteSchool([FromBody] QuickDeleteSchoolRequest req)
        {
            if (req == null || req.SchoolId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid school." });
            }

            var content = await LoadOrSeedCatalogContentAsync();
            var school = content.Schools.FirstOrDefault(s => s.Id == req.SchoolId);
            if (school == null)
            {
                return NotFound(new { success = false, message = "School not found." });
            }

            content.Schools.Remove(school);
            await SaveCatalogContentAsync(content);

            return Json(new { success = true, schoolId = req.SchoolId });
        }
    }
}

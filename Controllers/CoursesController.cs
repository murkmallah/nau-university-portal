using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;
using System.Globalization;

[Authorize(Roles = "Admin,Staff")]
public class CoursesController : Controller
{
    private readonly NauDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPermissionService _permissionService;

    public CoursesController(
        NauDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    // ================= PUBLIC-FACING PAGE (no login required) =================
    // GET: /Courses/Public -> accordion of Schools, each listing its Courses
    [AllowAnonymous]
    public async Task<IActionResult> Public(string? edit = null)
    {
        bool wantsEdit = !string.IsNullOrEmpty(edit) &&
            (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

        ApplicationUser? currentUser = null;
        bool hasSchoolPerm = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                hasSchoolPerm = await _permissionService.HasSchoolPermissionAsync(currentUser.Id, null)
                             || await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.SchoolAll)
                             || await _permissionService.IsSuperAdminAsync(currentUser.Id);
            }
        }

        bool canEdit = currentUser != null && (User.IsInRole("Admin") || hasSchoolPerm);
        ViewBag.CanEdit = canEdit;
        ViewBag.EditMode = wantsEdit && canEdit;
        ViewBag.UserPermissions = currentUser != null ? await _permissionService.GetUserPermissionsAsync(currentUser.Id) : new List<string>();
        ViewBag.IsSuperAdmin = currentUser != null && await _permissionService.IsSuperAdminAsync(currentUser.Id);

        ViewBag.CurrentUserName = "";
        if (currentUser != null)
        {
            ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (User.Identity?.Name ?? "");
        }

        await EnsureCalendarDataSeededAsync();

        var schools = await _context.Schools
            .Include(s => s.Courses)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        const string ctaSlug = "courses-public-cta";
        var ctaPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == ctaSlug);
        if (ctaPage == null)
        {
            ctaPage = new Page
            {
                Slug = ctaSlug,
                Title = "Ready to Advance Your Career?",
                ContentHtml = "Get in touch with our admissions team to learn more about short course registration.",
                IsPublished = true,
                UpdatedAt = DateTime.Now
            };
            _context.Pages.Add(ctaPage);
            await _context.SaveChangesAsync();
        }
        ViewBag.CtaTitle = ctaPage.Title;
        ViewBag.CtaText = ctaPage.ContentHtml;

        return View(schools);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> BySchool(string? school, int take = 4)
    {
        var keyword = (school ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(keyword))
        {
            return Json(new { schoolName = (string?)null, totalCourses = 0, courses = Array.Empty<object>() });
        }

        var schools = await _context.Schools
            .Include(s => s.Courses)
            .ToListAsync();

        var match = schools.FirstOrDefault(s =>
            !string.IsNullOrEmpty(s.Name) && s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return Json(new { schoolName = (string?)null, totalCourses = 0, courses = Array.Empty<object>() });
        }

        var count = Math.Max(1, Math.Min(take, 10));
        var courses = match.Courses
            .OrderBy(c => c.CourseId)
            .Take(count)
            .Select(c => new { id = c.CourseId, title = c.Title })
            .ToList();

        return Json(new
        {
            schoolName = match.Name,
            totalCourses = match.Courses.Count,
            courses
        });
    }

    public class QuickDeleteSchoolRequest
    {
        public int SchoolId { get; set; }
    }

    // POST: /Courses/QuickDeleteSchool  (Admin only, Edit Mode only -- removes
    // an accordion block/School. Refuses if the school still has courses in it,
    // to avoid silently wiping out a whole school's course list by mistake.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteSchool([FromBody] QuickDeleteSchoolRequest req)
    {
        if (req == null || req.SchoolId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid school." });
        }

        var school = await _context.Schools
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.SchoolId == req.SchoolId);

        if (school == null)
        {
            return NotFound(new { success = false, message = "School not found (it may already be deleted)." });
        }

        if (school.Courses.Count > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Can't delete \"{school.Name}\" -- it still has {school.Courses.Count} course(s). Delete those courses first."
            });
        }

        _context.Schools.Remove(school);
        await _context.SaveChangesAsync();

        return Json(new { success = true, schoolId = req.SchoolId });
    }

    public class QuickUpdateSchoolRequest
    {
        public int SchoolId { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? ColorHex { get; set; }
    }

    // POST: /Courses/QuickUpdateSchool  (Admin only, Edit Mode only -- edits an
    // existing School's name/icon/color from the live page.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateSchool([FromBody] QuickUpdateSchoolRequest req)
    {
        if (req == null || req.SchoolId <= 0 || string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "School name is required." });
        }

        var school = await _context.Schools.FindAsync(req.SchoolId);
        if (school == null)
        {
            return NotFound(new { success = false, message = "School not found." });
        }

        school.Name = req.Name.Trim();
        school.Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-graduation-cap" : req.Icon.Trim();
        school.ColorHex = string.IsNullOrWhiteSpace(req.ColorHex) ? "#0b3a5c" : req.ColorHex.Trim();

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            schoolId = school.SchoolId,
            name = school.Name,
            icon = school.Icon,
            colorHex = school.ColorHex
        });
    }

    public class QuickAddSchoolRequest
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? ColorHex { get; set; }
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

        int nextOrder = (await _context.Schools.AnyAsync())
            ? await _context.Schools.MaxAsync(s => s.DisplayOrder) + 1
            : 0;

        var school = new School
        {
            Name = req.Name.Trim(),
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-graduation-cap" : req.Icon.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(req.ColorHex) ? "#0b3a5c" : req.ColorHex.Trim(),
            DisplayOrder = nextOrder
        };

        _context.Schools.Add(school);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            schoolId = school.SchoolId,
            name = school.Name,
            icon = school.Icon,
            colorHex = school.ColorHex
        });
    }

    public class QuickUpdateCtaRequest
    {
        public string? Title { get; set; }
        public string? Text { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateCta([FromBody] QuickUpdateCtaRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Heading is required." });
        }

        const string ctaSlug = "courses-public-cta";
        var ctaPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == ctaSlug);
        if (ctaPage == null)
        {
            ctaPage = new Page { Slug = ctaSlug, Title = req.Title.Trim(), ContentHtml = (req.Text ?? string.Empty).Trim(), IsPublished = true, UpdatedAt = DateTime.Now };
            _context.Pages.Add(ctaPage);
        }
        else
        {
            ctaPage.Title = req.Title.Trim();
            ctaPage.ContentHtml = (req.Text ?? string.Empty).Trim();
            ctaPage.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return Json(new { success = true, title = ctaPage.Title, text = ctaPage.ContentHtml });
    }

    private static (string Description, string Topics, string Highlights, string Certificate) GetDefaultCourseContent(string schoolName)
    {
        var n = schoolName ?? string.Empty;

        if (n.Contains("Navigation", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "The School of Air Navigation Services delivers specialized training in Air Traffic Control and aviation communication systems aligned with ICAO standards.",
                "Air Traffic Control; Radar Operations; CNS/ATM Systems; ICAO Procedures; Aerodrome Control; Approach Control",
                "ICAO Doc 9868 (PANS-TRG) aligned curriculum; Simulation-based ATC training with real radar systems; Bilingual instruction (English & Urdu) for technical modules; Recognized by Civil Aviation Authority (CAA) Pakistan; Practical exercises in live ATC environment",
                "NAU Certified");
        }
        if (n.Contains("Regulatory", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "The School of Regulatory Services provides training in aviation safety, airworthiness, and regulatory compliance in line with national and international aviation standards.",
                "Safety Management Systems; Airworthiness; CAA Regulations; SMS Implementation; Accident Investigation; Compliance Auditing",
                "Aligned with ICAO Annex 19 — Safety Management; Case studies from real aviation incidents; Certification recognized by CAA Pakistan; Covers both operational and administrative safety roles; Regular updates per latest ICAO and IATA standards",
                "NAU Certified");
        }
        if (n.Contains("Airport", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "The School of Airport Services trains aviation professionals in ground operations, rescue & firefighting, and airport emergency procedures.",
                "ARFF Operations; Ground Handling; Airport Emergency Planning; Dangerous Goods; Passenger Handling; Ramp Safety",
                "Hands-on fire simulation and rescue drills; ICAO Doc 9137 Airport Services Manual compliance; Live airport environment practical sessions; Covers Aerodrome Emergency Plan (AEP) procedures; Internationally benchmarked assessment criteria",
                "NAU Certified");
        }
        if (n.Contains("Management", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "The School of Aviation Management develops leadership, management, and soft skills for aviation professionals at all organizational levels.",
                "Leadership & Management; Emotional Intelligence; Ethics in Aviation; HR Development; Organizational Behavior; Communication Skills",
                "Designed for both officers and staff cadres; Facilitated by senior aviation management professionals; Interactive workshops and group activities; Applicable across all aviation departments; Focus on real-world aviation organizational challenges",
                "NAU Certified");
        }

        return (
            $"This course is delivered by {n} at National Aviation University, providing industry-focused, certification-based training for aviation professionals.",
            "Aviation Fundamentals; Industry Standards; Practical Training",
            "Certified by National Aviation University; Industry-relevant curriculum; Delivered by experienced faculty",
            "NAU Certified");
    }

    public class QuickAddCourseRequest
    {
        public int SchoolId { get; set; }
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? Description { get; set; }
        public string? Topics { get; set; }
        public string? Highlights { get; set; }
        public string? Certificate { get; set; }
    }

    // POST: /Courses/QuickAdd (Admin & Staff)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAdd([FromBody] QuickAddCourseRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Course title is required." });
        }

        var school = await _context.Schools.FindAsync(req.SchoolId);
        if (school == null)
        {
            return BadRequest(new { success = false, message = "Invalid school." });
        }

        var start = TryParseMonthYear(req.StartDate);
        var end = TryParseMonthYear(req.EndDate);
        var defaults = GetDefaultCourseContent(school.Name);

        var course = new Course
        {
            SchoolId = req.SchoolId,
            Title = req.Title.Trim(),
            Duration = string.IsNullOrWhiteSpace(req.Duration) ? null : req.Duration.Trim(),
            StartDate = start,
            EndDate = end,
            IsTbd = start == null || end == null,
            Description = string.IsNullOrWhiteSpace(req.Description) ? defaults.Description : req.Description.Trim(),
            Topics = string.IsNullOrWhiteSpace(req.Topics) ? defaults.Topics : req.Topics.Trim(),
            Highlights = string.IsNullOrWhiteSpace(req.Highlights) ? defaults.Highlights : req.Highlights.Trim(),
            Certificate = string.IsNullOrWhiteSpace(req.Certificate) ? "NAU Certified" : req.Certificate.Trim(),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            courseId = course.CourseId,
            schoolId = course.SchoolId,
            title = course.Title,
            duration = course.Duration ?? "-",
            startDate = course.IsTbd || course.StartDate == null ? "TBD" : course.StartDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            endDate = course.IsTbd || course.EndDate == null ? "TBD" : course.EndDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            description = course.Description,
            topics = course.Topics,
            highlights = course.Highlights,
            certificate = course.Certificate,
            schoolName = school.Name
        });
    }

    public class QuickUpdateCourseRequest
    {
        public int CourseId { get; set; }
        public string? Title { get; set; }
        public string? Duration { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? Description { get; set; }
        public string? Topics { get; set; }
        public string? Highlights { get; set; }
        public string? Certificate { get; set; }
    }

    // POST: /Courses/QuickUpdateCourse (Admin & Staff)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateCourse([FromBody] QuickUpdateCourseRequest req)
    {
        if (req == null || req.CourseId <= 0 || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Course title is required." });
        }

        var course = await _context.Courses.Include(c => c.School).FirstOrDefaultAsync(c => c.CourseId == req.CourseId);
        if (course == null)
        {
            return NotFound(new { success = false, message = "Course not found." });
        }

        var start = TryParseMonthYear(req.StartDate);
        var end = TryParseMonthYear(req.EndDate);

        course.Title = req.Title.Trim();
        course.Duration = string.IsNullOrWhiteSpace(req.Duration) ? null : req.Duration.Trim();
        course.StartDate = start;
        course.EndDate = end;
        course.IsTbd = start == null || end == null;
        course.Description = req.Description?.Trim();
        course.Topics = req.Topics?.Trim();
        course.Highlights = req.Highlights?.Trim();
        course.Certificate = string.IsNullOrWhiteSpace(req.Certificate) ? "NAU Certified" : req.Certificate.Trim();
        course.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            courseId = course.CourseId,
            schoolId = course.SchoolId,
            title = course.Title,
            duration = course.Duration ?? "-",
            startDate = course.IsTbd || course.StartDate == null ? "TBD" : course.StartDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            endDate = course.IsTbd || course.EndDate == null ? "TBD" : course.EndDate.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
            description = course.Description,
            topics = course.Topics,
            highlights = course.Highlights,
            certificate = course.Certificate,
            schoolName = course.School?.Name
        });
    }

    public class QuickDeleteRequest
    {
        public int CourseId { get; set; }
    }

    // POST: /Courses/QuickDelete  (Admin only -- lets the live "🗑" button
    // on the public page remove a Course row without leaving the page)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDelete([FromBody] QuickDeleteRequest req)
    {
        if (req == null || req.CourseId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid course." });
        }

        var course = await _context.Courses.FindAsync(req.CourseId);
        if (course == null)
        {
            return NotFound(new { success = false, message = "Course not found (it may already be deleted)." });
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return Json(new { success = true, courseId = req.CourseId });
    }

    private static DateOnly? TryParseMonthYear(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("TBD", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string trimmed = text.Trim();
        string[] formats = new string[]
        {
            "dd MMM yyyy", "d MMM yyyy", "dd MMMM yyyy", "d MMMM yyyy",
            "dd-MMM-yyyy", "d-MMM-yyyy", "dd/MM/yyyy", "d/M/yyyy",
            "yyyy-MM-dd", "MMM yyyy", "MMMM yyyy"
        };

        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
        {
            return DateOnly.FromDateTime(dtExact);
        }
        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtAny))
        {
            return DateOnly.FromDateTime(dtAny);
        }
        return null;
    }

    // =============================================================================

    // GET: COURSES
    public async Task<IActionResult> Index()
    {
        return View(await _context.Courses.ToListAsync());
    }

    // GET: COURSES/Details/5
    public async Task<IActionResult> Details(int? courseid)
    {
        if (courseid == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(m => m.CourseId == courseid);
        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    // GET: COURSES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: COURSES/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CourseId,SchoolId,Title,Duration,StartDate,EndDate,IsTbd,CreatedAt,UpdatedAt,AcademicCalendarResults,School")] Course course)
    {
        if (ModelState.IsValid)
        {
            _context.Add(course);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(course);
    }

    // GET: COURSES/Edit/5
    public async Task<IActionResult> Edit(int? courseid)
    {
        if (courseid == null)
        {
            return NotFound();
        }

        var course = await _context.Courses.FindAsync(courseid);
        if (course == null)
        {
            return NotFound();
        }
        return View(course);
    }

    // POST: COURSES/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? courseid, [Bind("CourseId,SchoolId,Title,Duration,StartDate,EndDate,IsTbd,CreatedAt,UpdatedAt,AcademicCalendarResults,School")] Course course)
    {
        if (courseid != course.CourseId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(course);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(course.CourseId))
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
        return View(course);
    }

    // GET: COURSES/Delete/5
    public async Task<IActionResult> Delete(int? courseid)
    {
        if (courseid == null)
        {
            return NotFound();
        }

        var course = await _context.Courses
            .FirstOrDefaultAsync(m => m.CourseId == courseid);
        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    // POST: COURSES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? courseid)
    {
        var course = await _context.Courses.FindAsync(courseid);
        if (course != null)
        {
            _context.Courses.Remove(course);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool CourseExists(int? courseid)
    {
        return _context.Courses.Any(e => e.CourseId == courseid);
    }

    // GET: /Courses/AcademicCalendarData -> JSON endpoint for site-wide Academic Calendar modal
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> AcademicCalendarData()
    {
        await EnsureCalendarDataSeededAsync();

        var schoolsList = await _context.Schools
            .Include(s => s.Courses)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var schoolsData = schoolsList.Select(s => new
        {
            name = s.Name,
            icon = string.IsNullOrWhiteSpace(s.Icon) ? "fa-graduation-cap" : s.Icon,
            color = string.IsNullOrWhiteSpace(s.ColorHex) ? "#0b2c4d" : s.ColorHex,
            courses = s.Courses.Select(c => new
            {
                id = c.CourseId,
                title = c.Title,
                duration = string.IsNullOrWhiteSpace(c.Duration) ? "01 Week" : c.Duration,
                start = c.IsTbd || !c.StartDate.HasValue ? "TBD" : c.StartDate.Value.ToString("dd.MM.yyyy"),
                end = c.IsTbd || !c.EndDate.HasValue ? "TBD" : c.EndDate.Value.ToString("dd.MM.yyyy")
            }).ToList()
        }).ToList();

        var resultsList = await _context.AcademicCalendarResults
            .OrderBy(r => r.ResultDate)
            .ToListAsync();

        var resultDatesData = resultsList.Select(r => new
        {
            id = r.ResultId,
            label = r.Label,
            date = r.ResultDate.ToString("dd.MM.yyyy"),
            type = string.IsNullOrWhiteSpace(r.ResultType) ? "result" : r.ResultType.ToLowerInvariant()
        }).ToList();

        return Json(new
        {
            schools = schoolsData,
            resultDates = resultDatesData
        });
    }

    private async Task EnsureCalendarDataSeededAsync()
    {
        var hasOldDummyDates = await _context.Courses.AnyAsync() &&
            await _context.Courses.Where(c => !c.IsTbd && c.StartDate != null).AllAsync(c => c.StartDate!.Value.Day == 1);

        if (await _context.Schools.AnyAsync() && !hasOldDummyDates)
        {
            return;
        }

        if (hasOldDummyDates)
        {
            _context.AcademicCalendarResults.RemoveRange(_context.AcademicCalendarResults);
            _context.Courses.RemoveRange(_context.Courses);
            _context.Schools.RemoveRange(_context.Schools);
            await _context.SaveChangesAsync();
        }

        var s1 = new School { Name = "School of Air Navigation Services (ANS)", Icon = "fas fa-tower-cell", ColorHex = "#0b2c4d", DisplayOrder = 1 };
        var s2 = new School { Name = "School of Regulatory Services", Icon = "fas fa-scale-balanced", ColorHex = "#1e5f74", DisplayOrder = 2 };
        var s3 = new School { Name = "School of Airport Services", Icon = "fas fa-fire-extinguisher", ColorHex = "#c0392b", DisplayOrder = 3 };
        var s4 = new School { Name = "School of Aviation Management", Icon = "fas fa-chart-line", ColorHex = "#1e8e3e", DisplayOrder = 4 };

        _context.Schools.AddRange(s1, s2, s3, s4);
        await _context.SaveChangesAsync();

        var now = DateTime.Now;

        // School 1 Courses
        var cS1 = new List<Course>
        {
            new Course { SchoolId = s1.SchoolId, Title = "Basic ATC Course No. 46", Duration = "40 Weeks", StartDate = new DateOnly(2026, 12, 7), EndDate = new DateOnly(2027, 9, 3), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.29", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.30", Duration = "01 Week", StartDate = new DateOnly(2026, 3, 30), EndDate = new DateOnly(2026, 4, 3), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.31", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 25), EndDate = new DateOnly(2026, 5, 29), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.32", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 6), EndDate = new DateOnly(2026, 7, 10), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.33", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.34", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 31), EndDate = new DateOnly(2026, 9, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.35", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 19), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.36", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.37", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.38", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 30), EndDate = new DateOnly(2026, 12, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.39", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 14), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Officers — No.40", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 28), EndDate = new DateOnly(2027, 1, 1), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Assistant Course No. 03", Duration = "08 Weeks", StartDate = new DateOnly(2026, 9, 7), EndDate = new DateOnly(2026, 10, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Staff — No.09", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 26), EndDate = new DateOnly(2026, 1, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Staff — No.10", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 18), EndDate = new DateOnly(2026, 5, 22), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Staff — No.11", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATC Refresher (Abridged) For Staff — No.12", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 23), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Approach Control Surveillance Course No. 06", Duration = "05 Weeks", StartDate = new DateOnly(2026, 2, 9), EndDate = new DateOnly(2026, 3, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Area / Approach Control Surveillance Course No. 60", Duration = "12 Weeks", StartDate = new DateOnly(2026, 4, 13), EndDate = new DateOnly(2026, 7, 3), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Area / Approach Control Surveillance Course No. 61", Duration = "12 Weeks", StartDate = new DateOnly(2026, 7, 27), EndDate = new DateOnly(2026, 10, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Flight Procedure Design (Conventional) Course No. 03", Duration = "04 Weeks", StartDate = new DateOnly(2026, 6, 8), EndDate = new DateOnly(2026, 7, 3), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Flight Procedure Design (PBN) Course No. 01", Duration = "03 Weeks", StartDate = new DateOnly(2026, 10, 12), EndDate = new DateOnly(2026, 10, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ELP (English Language Proficiency) Advance Course No. 02", Duration = "02 Weeks", StartDate = new DateOnly(2026, 4, 6), EndDate = new DateOnly(2026, 4, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ELP (English Language Proficiency) Course No. 05", Duration = "03 Weeks", StartDate = new DateOnly(2026, 8, 10), EndDate = new DateOnly(2026, 9, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Basic SAR Course No. 22", Duration = "02 Weeks", StartDate = new DateOnly(2026, 3, 2), EndDate = new DateOnly(2026, 3, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Basic SAR Course No. 23", Duration = "02 Weeks", StartDate = new DateOnly(2026, 8, 31), EndDate = new DateOnly(2026, 9, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Basic SAR Course No. 24", Duration = "02 Weeks", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "SAR Coordinator Course No. 02", Duration = "02 Weeks", StartDate = new DateOnly(2026, 11, 30), EndDate = new DateOnly(2026, 12, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "On Job Training (OJT) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 12), EndDate = new DateOnly(2026, 1, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "On Job Training (OJT) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 11), EndDate = new DateOnly(2026, 5, 15), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "On Job Training (OJT) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 30), EndDate = new DateOnly(2026, 12, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Managing Aeronautical Information Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 24), EndDate = new DateOnly(2026, 8, 28), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Managing Aeronautical Information Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 14), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "CAVRN (For Officers) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 1, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "CAVRN (For Officers) Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 5), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "SWIM Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 20), EndDate = new DateOnly(2026, 4, 24), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "SWIM Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 2, 9), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Aeronautical Radio Freq. Spectrum Mgmt (Officers) No. 05", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "COO Course No. 09", Duration = "14 Weeks", IsTbd = true, CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Communication Operation Assistant Course No. 21", Duration = "10 Weeks", IsTbd = true, CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "IDS AMHS Advance (For Officers) Course No. 04", Duration = "02 Weeks", StartDate = new DateOnly(2026, 7, 27), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 08", Duration = "02 Weeks", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "IDS AirNav AMHS (CRONOS) DB (Staff) Course No. 09", Duration = "02 Weeks", StartDate = new DateOnly(2026, 12, 7), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "SDCPS Software (IOU) (Staff) Course No. 10", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 6), EndDate = new DateOnly(2026, 7, 10), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "SDCPS Software (IOU) (Staff) Course No. 11", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Aeronautical Mobile Service (Officers) Course No. 04", Duration = "04 Weeks", StartDate = new DateOnly(2026, 4, 27), EndDate = new DateOnly(2026, 5, 22), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Aeronautical Mobile Service (Staff) Course No. 05", Duration = "03 Weeks", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 9, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "INDRA MSSR Operation & Maintenance Course No. 05", Duration = "04 Weeks", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ADSB Operation & Maintenance Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 3, 2), EndDate = new DateOnly(2026, 3, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Selex (ILS) Operation & Maintenance Course No. 03", Duration = "04 Weeks", StartDate = new DateOnly(2026, 3, 30), EndDate = new DateOnly(2026, 4, 24), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "INDRA PSR Operation & Maintenance Course No. 03", Duration = "04 Weeks", StartDate = new DateOnly(2026, 4, 6), EndDate = new DateOnly(2026, 4, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATM (Air Traffic Management) System Course No. 03", Duration = "02 Weeks", StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 5, 15), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ELDS MSSR Operation & Maintenance Course No. 04", Duration = "04 Weeks", StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 8, 14), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Selex DVOR / DME Operation & Maintenance Course No. 04", Duration = "04 Weeks", StartDate = new DateOnly(2026, 11, 23), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "Basic ATSEP Refresher (For Officers) Course No. 06", Duration = "01 Week", StartDate = new DateOnly(2026, 2, 23), EndDate = new DateOnly(2026, 2, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATSEP Data Processing & Networking (Technicians) No. 03", Duration = "08 Weeks", StartDate = new DateOnly(2026, 6, 8), EndDate = new DateOnly(2026, 7, 31), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATSEP (Radar) For Technicians Course No. 03", Duration = "07 Weeks", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 10, 2), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATSEP (NAVAIDS) For Technicians Course No. 02", Duration = "06 Weeks", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "ATSEP (Radio Communication) For Technicians Course No. 03", Duration = "06 Weeks", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 11, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "INDRA VCCS SDC-2000 O&M Course No. 01", Duration = "01 Week", IsTbd = true, CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s1.SchoolId, Title = "HARRIS LIBERTY STAR VCCS O&M Course No. 01", Duration = "01 Week", IsTbd = true, CreatedAt = now, UpdatedAt = now }
        };

        // School 2 Courses
        var cS2 = new List<Course>
        {
            new Course { SchoolId = s2.SchoolId, Title = "International Air Law Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 5, 8), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Managing Compliance with ICAO SARPs Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 18), EndDate = new DateOnly(2026, 5, 22), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Air Cargo Regulatory Framework & Compliance Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 5), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Annex 14 Application and Requirements Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aerodrome Certification Course No. 02", Duration = "02 Weeks", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 12), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aerodrome Manual Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 29), EndDate = new DateOnly(2026, 7, 3), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aviation Economics Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 1, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Supply & Demand in Air Transport Industry Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 2, 8), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Airport Economics Course No. 01", Duration = "02 Weeks", StartDate = new DateOnly(2026, 7, 6), EndDate = new DateOnly(2026, 7, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Airport Marketing Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 8, 21), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Audit Technique Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Audit Technique Course No. 05", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 23), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aeronautical Study Safety Assessment Course No. 03 (Jan)", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 12), EndDate = new DateOnly(2026, 1, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aeronautical Study Safety Assessment Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 7), EndDate = new DateOnly(2026, 9, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Understanding ICAO Annex-19 Safety Management Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 20), EndDate = new DateOnly(2026, 4, 24), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Understanding ICAO Annex-19 Safety Management Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 7), EndDate = new DateOnly(2026, 12, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Fundamentals of Air Cargo Operations & Safety (Staff) No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 13), EndDate = new DateOnly(2026, 7, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "ANS Inspector Course No. 01", Duration = "02 Weeks", StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 7, 31), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Rules of the Air for Aviation Safety Professionals (ICAO Annex 02) No. 01", Duration = "02 Weeks", StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aviation Climate Change Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Air Passenger Rights Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 3, 30), EndDate = new DateOnly(2026, 4, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Aircraft Accident and Investigation Awareness (ICAO Annex-13) No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 31), EndDate = new DateOnly(2026, 9, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Facilitation (ICAO Annex-9) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s2.SchoolId, Title = "Disabled Aircraft Recovery Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 19), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now }
        };

        // School 3 Courses
        var cS3 = new List<Course>
        {
            new Course { SchoolId = s3.SchoolId, Title = "Senior Aerodrome Fire Fighter Course No. 57", Duration = "05 Weeks", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 2, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Senior Aerodrome Fire Fighter Course No. 58", Duration = "05 Weeks", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 9, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Senior Aerodrome Fire Fighter Course No. 59", Duration = "05 Weeks", StartDate = new DateOnly(2026, 10, 26), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Aerodrome Fire Fighter Refresher Course No. 12", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 20), EndDate = new DateOnly(2026, 4, 24), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Aerodrome Fire Fighter Refresher Course No. 13", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 21), EndDate = new DateOnly(2026, 9, 25), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Aerodrome Fire Fighter Refresher Course No. 14", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 14), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "SVD (Refresher) Course No. 10", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 20), EndDate = new DateOnly(2026, 4, 24), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "SVD (Refresher) Course No. 11", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 21), EndDate = new DateOnly(2026, 9, 25), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "SVD (Refresher) Course No. 12", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 14), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 26), EndDate = new DateOnly(2026, 1, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 15), EndDate = new DateOnly(2026, 6, 19), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 31), EndDate = new DateOnly(2026, 9, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 05", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 7), EndDate = new DateOnly(2026, 9, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Fighting (Corporate Sector) Course No. 06", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 19), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Prevention Course No. 69", Duration = "03 Weeks", StartDate = new DateOnly(2026, 2, 23), EndDate = new DateOnly(2026, 3, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Prevention Course No. 70", Duration = "03 Weeks", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 19), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Prevention Course No. 71", Duration = "03 Weeks", StartDate = new DateOnly(2026, 6, 29), EndDate = new DateOnly(2026, 7, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Breathing Apparatus Course No. 24", Duration = "03 Weeks", StartDate = new DateOnly(2026, 3, 30), EndDate = new DateOnly(2026, 4, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Breathing Apparatus Course No. 25", Duration = "03 Weeks", StartDate = new DateOnly(2026, 4, 27), EndDate = new DateOnly(2026, 5, 15), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Breathing Apparatus Course No. 26", Duration = "03 Weeks", StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Junior Fire Officer Course No. 15", Duration = "05 Weeks", StartDate = new DateOnly(2026, 9, 28), EndDate = new DateOnly(2026, 10, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Investigation (For Officers) Course No. 01", Duration = "02 Weeks", StartDate = new DateOnly(2026, 11, 30), EndDate = new DateOnly(2026, 12, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Aircraft Familiarization (For Officers) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 28), EndDate = new DateOnly(2027, 1, 1), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Fire Officer (Refresher) Course No. 14", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Aerodrome Operations (For Officers) Course No. 06", Duration = "04 Weeks", StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Facilitation & Hospitality Management (Officers) Course No. 03", Duration = "02 Weeks", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 25), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Facilitation & Hospitality Management (Staff) Course No. 07", Duration = "02 Weeks", StartDate = new DateOnly(2026, 3, 10), EndDate = new DateOnly(2026, 3, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Facilitation & Hospitality Management (Officers) Course No. 08", Duration = "02 Weeks", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 07 (JIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 08 (IIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 1, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 09 (QIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 6), EndDate = new DateOnly(2026, 7, 10), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 10 (BKIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 3), EndDate = new DateOnly(2026, 8, 7), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 11 (FIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 12 (AIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airside Safety Awareness Course No. 13 (MIAP)", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Marshaller Refresher Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 2, 16), EndDate = new DateOnly(2026, 2, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Marshaller Refresher Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 20), EndDate = new DateOnly(2026, 7, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Standard & Recommended Practices Aerodromes (Annex-14) (Engineers) No. 03", Duration = "02 Weeks", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Airfield Pavement Maintenance & Surface Evaluation (Engineers) No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 6), EndDate = new DateOnly(2026, 4, 10), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "PBB Maintenance (STP) Course No. 08", Duration = "02 Weeks", StartDate = new DateOnly(2026, 2, 2), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "PBB Maintenance (STP) Course No. 09", Duration = "02 Weeks", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Underground Cable Fault Repairs (Technicians) Course No. 03", Duration = "02 Weeks", StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 5, 15), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "Engineering Estimation (Staff) Course No. 02", Duration = "02 Weeks", StartDate = new DateOnly(2026, 6, 8), EndDate = new DateOnly(2026, 6, 19), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s3.SchoolId, Title = "FIDIC Contract (Officers) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 8, 21), CreatedAt = now, UpdatedAt = now }
        };

        // School 4 Courses
        var cS4 = new List<Course>
        {
            new Course { SchoolId = s4.SchoolId, Title = "Aviation Management Course No. 09", Duration = "05 Weeks", StartDate = new DateOnly(2026, 1, 12), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Aviation Management Course No. 10", Duration = "05 Weeks", StartDate = new DateOnly(2026, 10, 19), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Personal Effectiveness (Staff) Course No. 06", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 5, 8), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Personal Effectiveness (Staff) Course No. 07", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 7), EndDate = new DateOnly(2026, 9, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Personal Effectiveness (Staff) Course No. 08", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 7), EndDate = new DateOnly(2026, 12, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Logistics (Staff) Course No. 05", Duration = "03 Weeks", StartDate = new DateOnly(2026, 1, 19), EndDate = new DateOnly(2026, 2, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Logistics (Staff) Course No. 06", Duration = "03 Weeks", StartDate = new DateOnly(2026, 4, 20), EndDate = new DateOnly(2026, 5, 8), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Logistics (Staff) Course No. 07", Duration = "03 Weeks", StartDate = new DateOnly(2026, 7, 13), EndDate = new DateOnly(2026, 7, 31), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Logistics (Staff) Course No. 08", Duration = "03 Weeks", StartDate = new DateOnly(2026, 11, 9), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Logistics & Supply Chain Operations (Officers) No. 01", Duration = "03 Weeks", StartDate = new DateOnly(2026, 5, 18), EndDate = new DateOnly(2026, 6, 5), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Logistics & Supply Chain Operations (Officers) No. 02", Duration = "03 Weeks", StartDate = new DateOnly(2026, 9, 7), EndDate = new DateOnly(2026, 9, 25), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Human Factor in Aviation (Officers) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 5, 4), EndDate = new DateOnly(2026, 5, 8), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Human Factor in Aviation (Officers) Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 26), EndDate = new DateOnly(2026, 10, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Introduction to Aviation IT System (Staff) Course No. 05", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 22), EndDate = new DateOnly(2026, 6, 26), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Introduction to Aviation IT System (Staff) Course No. 06", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 23), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 14), EndDate = new DateOnly(2026, 9, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "IT/Cyber Security, Audit & Compliance Mgmt (Officers) No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 7), EndDate = new DateOnly(2026, 12, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "MS Excel Basic (Staff) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 13), EndDate = new DateOnly(2026, 4, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "MS Excel Basic (Staff) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 19), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Introduction to ERP (Staff) Course No. 01", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 16), EndDate = new DateOnly(2026, 11, 20), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Introduction to ERP (Staff) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 12, 14), EndDate = new DateOnly(2026, 12, 18), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "E-Procurement through E-PADS (Officers) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "E-Procurement through E-PADS (Officers) Course No. 04", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Office Organization (Asst/Supervisor) Course No. 21", Duration = "02 Weeks", StartDate = new DateOnly(2026, 1, 5), EndDate = new DateOnly(2026, 1, 16), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Office Organization (Asst/Supervisor) Course No. 22", Duration = "02 Weeks", StartDate = new DateOnly(2026, 8, 31), EndDate = new DateOnly(2026, 9, 11), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Legal Proficiency (Officers) Course No. 01", Duration = "03 Weeks", StartDate = new DateOnly(2026, 1, 26), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Legal Proficiency (Staff) Course No. 02", Duration = "02 Weeks", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 8, 28), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Legal Proficiency (Staff) Course No. 03", Duration = "02 Weeks", StartDate = new DateOnly(2026, 10, 12), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "ITC Refresher Course No. 02", Duration = "03 Days", StartDate = new DateOnly(2026, 2, 4), EndDate = new DateOnly(2026, 2, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "ITC Course No. 33", Duration = "02 Weeks", StartDate = new DateOnly(2026, 4, 6), EndDate = new DateOnly(2026, 4, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "ITC Course No. 34", Duration = "02 Weeks", StartDate = new DateOnly(2026, 10, 12), EndDate = new DateOnly(2026, 10, 23), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "ETC (Examination Technique Course) No. 14", Duration = "01 Week", StartDate = new DateOnly(2026, 6, 8), EndDate = new DateOnly(2026, 6, 12), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "ETC (Examination Technique Course) No. 15", Duration = "01 Week", StartDate = new DateOnly(2026, 8, 17), EndDate = new DateOnly(2026, 8, 21), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 02 (SQMS)", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 6), EndDate = new DateOnly(2026, 4, 10), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Mgmt System Standards (ISO/SMS) Officers Course No. 03 (SQMS)", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 30), EndDate = new DateOnly(2026, 12, 4), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 03 (SQMS)", Duration = "01 Week", StartDate = new DateOnly(2026, 4, 13), EndDate = new DateOnly(2026, 4, 17), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Basic Mgmt System Standards (ISO/SMS) Staff Course No. 04 (SQMS)", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 26), EndDate = new DateOnly(2026, 10, 30), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Finance For Non-Financial Managers (Officers) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 9, 21), EndDate = new DateOnly(2026, 9, 25), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Finance For Non-Financial Managers (Officers) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 2), EndDate = new DateOnly(2026, 11, 6), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "PPRA Rules (Officers) Course No. 02", Duration = "01 Week", StartDate = new DateOnly(2026, 7, 27), EndDate = new DateOnly(2026, 7, 31), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "PPRA Rules (Officers) Course No. 03", Duration = "01 Week", StartDate = new DateOnly(2026, 10, 5), EndDate = new DateOnly(2026, 10, 9), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Superintendent Development Course No. 05", Duration = "01 Week", StartDate = new DateOnly(2026, 2, 9), EndDate = new DateOnly(2026, 2, 13), CreatedAt = now, UpdatedAt = now },
            new Course { SchoolId = s4.SchoolId, Title = "Superintendent Development Course No. 06", Duration = "01 Week", StartDate = new DateOnly(2026, 11, 23), EndDate = new DateOnly(2026, 11, 27), CreatedAt = now, UpdatedAt = now }
        };

        _context.Courses.AddRange(cS1);
        _context.Courses.AddRange(cS2);
        _context.Courses.AddRange(cS3);
        _context.Courses.AddRange(cS4);
        await _context.SaveChangesAsync();

        if (!await _context.AcademicCalendarResults.AnyAsync())
        {
            var results = new List<AcademicCalendarResult>
            {
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.29 — Result Announced", ResultDate = new DateOnly(2026, 1, 12), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.30 — Result Announced", ResultDate = new DateOnly(2026, 4, 6), ResultType = "result" },
                new AcademicCalendarResult { Label = "Audit Technique Course No.04 — Result Announced", ResultDate = new DateOnly(2026, 1, 13), ResultType = "result" },
                new AcademicCalendarResult { Label = "Aeronautical Study Safety Assessment No.03 (Jan) — Result Announced", ResultDate = new DateOnly(2026, 1, 20), ResultType = "result" },
                new AcademicCalendarResult { Label = "Aviation Economics Course No.03 — Result Announced", ResultDate = new DateOnly(2026, 1, 27), ResultType = "result" },
                new AcademicCalendarResult { Label = "INDRA MSSR O&M No.05 — Result Announced", ResultDate = new DateOnly(2026, 2, 3), ResultType = "result" },
                new AcademicCalendarResult { Label = "Supply & Demand in Air Transport Industry No.02 — Result Announced", ResultDate = new DateOnly(2026, 2, 17), ResultType = "result" },
                new AcademicCalendarResult { Label = "Basic ATSEP Refresher Officers No.06 — Result Announced", ResultDate = new DateOnly(2026, 3, 3), ResultType = "result" },
                new AcademicCalendarResult { Label = "Approach Control Surveillance No.06 — Result Announced", ResultDate = new DateOnly(2026, 3, 20), ResultType = "result" },
                new AcademicCalendarResult { Label = "Air Passenger Rights No.01 — Result Announced", ResultDate = new DateOnly(2026, 4, 9), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.31 — Result Announced", ResultDate = new DateOnly(2026, 6, 4), ResultType = "result" },
                new AcademicCalendarResult { Label = "Area/Approach Control Surveillance No.60 — Mid-Term Result", ResultDate = new DateOnly(2026, 6, 1), ResultType = "midterm" },
                new AcademicCalendarResult { Label = "Aerodrome Certification Course No.02 — Result Announced", ResultDate = new DateOnly(2026, 6, 16), ResultType = "result" },
                new AcademicCalendarResult { Label = "Flight Procedure Design (Conventional) No.03 — Result Announced", ResultDate = new DateOnly(2026, 7, 7), ResultType = "result" },
                new AcademicCalendarResult { Label = "Area/Approach Control Surveillance No.60 — Final Result Announced", ResultDate = new DateOnly(2026, 7, 10), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.32 — Result Announced", ResultDate = new DateOnly(2026, 7, 14), ResultType = "result" },
                new AcademicCalendarResult { Label = "Airport Economics Course No.01 — Result Announced", ResultDate = new DateOnly(2026, 7, 21), ResultType = "result" },
                new AcademicCalendarResult { Label = "ANS Inspector Course No.01 — Result Announced", ResultDate = new DateOnly(2026, 8, 4), ResultType = "result" },
                new AcademicCalendarResult { Label = "ELP Course No.05 — Mid-Term Result", ResultDate = new DateOnly(2026, 8, 24), ResultType = "midterm" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.33 — Result Announced", ResultDate = new DateOnly(2026, 8, 11), ResultType = "result" },
                new AcademicCalendarResult { Label = "Aeronautical Mobile Service (Staff) No.05 — Result Announced", ResultDate = new DateOnly(2026, 9, 8), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.34 — Result Announced", ResultDate = new DateOnly(2026, 9, 8), ResultType = "result" },
                new AcademicCalendarResult { Label = "Aircraft Accident & Investigation Awareness No.01 — Result Announced", ResultDate = new DateOnly(2026, 9, 8), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATSEP Radar Technicians No.03 — Mid-Term Result", ResultDate = new DateOnly(2026, 9, 14), ResultType = "midterm" },
                new AcademicCalendarResult { Label = "Facilitation (ICAO Annex-9) No.01 — Result Announced", ResultDate = new DateOnly(2026, 9, 22), ResultType = "result" },
                new AcademicCalendarResult { Label = "Flight Procedure Design (PBN) No.01 — Result Announced", ResultDate = new DateOnly(2026, 11, 3), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATSEP Radar Technicians No.03 — Final Result Announced", ResultDate = new DateOnly(2026, 10, 6), ResultType = "result" },
                new AcademicCalendarResult { Label = "Aviation Climate Change Course No.01 — Result Announced", ResultDate = new DateOnly(2026, 10, 13), ResultType = "result" },
                new AcademicCalendarResult { Label = "Disabled Aircraft Recovery Course No.01 — Result Announced", ResultDate = new DateOnly(2026, 10, 27), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.35 — Result Announced", ResultDate = new DateOnly(2026, 9, 22), ResultType = "result" },
                new AcademicCalendarResult { Label = "Area/Approach Control Surveillance No.61 — Final Result Announced", ResultDate = new DateOnly(2026, 10, 20), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.36 — Result Announced", ResultDate = new DateOnly(2026, 10, 13), ResultType = "result" },
                new AcademicCalendarResult { Label = "Annex 14 Application and Requirements No.03 — Result Announced", ResultDate = new DateOnly(2026, 11, 24), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.37 — Result Announced", ResultDate = new DateOnly(2026, 11, 24), ResultType = "result" },
                new AcademicCalendarResult { Label = "SAR Coordinator Course No.02 — Result Announced", ResultDate = new DateOnly(2026, 12, 15), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.38 — Result Announced", ResultDate = new DateOnly(2026, 12, 8), ResultType = "result" },
                new AcademicCalendarResult { Label = "ATC Refresher (Officers) No.39 — Result Announced", ResultDate = new DateOnly(2026, 12, 22), ResultType = "result" },
                new AcademicCalendarResult { Label = "Understanding ICAO Annex-19 Safety Mgmt No.03 — Result Announced", ResultDate = new DateOnly(2026, 12, 15), ResultType = "result" },
                new AcademicCalendarResult { Label = "Basic ATC Course No.46 — Mid-Term Result (Estimated)", ResultDate = new DateOnly(2027, 3, 8), ResultType = "midterm" }
            };
            _context.AcademicCalendarResults.AddRange(results);
            await _context.SaveChangesAsync();
        }
    }
}

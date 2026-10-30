using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

[Authorize(Roles = "Admin,Staff")]
public class FacultiesController : Controller
{
    private readonly NauDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPermissionService _permissionService;

    public FacultiesController(
        NauDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    // ================= PUBLIC-FACING PAGE (no login required) =================
    // GET: /Faculties/Public -> grid of all Faculties, each listing its Programs.
    [AllowAnonymous]
    public async Task<IActionResult> Public(string? edit = null)
    {
        bool wantsEdit = !string.IsNullOrEmpty(edit) &&
            (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

        ApplicationUser? currentUser = null;
        bool hasFacultyPerm = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                hasFacultyPerm = await _permissionService.HasFacultyPermissionAsync(currentUser.Id, null)
                              || await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.FacultyAll)
                              || await _permissionService.IsSuperAdminAsync(currentUser.Id);
            }
        }

        bool canEdit = currentUser != null && (User.IsInRole("Admin") || hasFacultyPerm);
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

        await EnsureStandardFacultiesSeededAsync();

        var faculties = await _context.Faculties
            .Include(f => f.Programs)
            .OrderBy(f => f.FacultyId)
            .ToListAsync();

        // Small editable text blocks on this page (the "Ready to Begin Your
        // Aviation Career?" strip) reuse the generic Pages table, same
        // mechanism as the Homepage/Short Courses page, keyed by its own
        // slug so it doesn't touch either of those.
        const string ctaSlug = "faculties-public-cta";
        var ctaPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == ctaSlug);
        if (ctaPage == null)
        {
            ctaPage = new Page
            {
                Slug = ctaSlug,
                Title = "Ready to Begin Your Aviation Career?",
                ContentHtml = "Apply now and join Pakistan's premier aviation university — shaping the future of aviation.",
                IsPublished = true,
                UpdatedAt = DateTime.Now
            };
            _context.Pages.Add(ctaPage);
            await _context.SaveChangesAsync();
        }
        ViewBag.CtaTitle = ctaPage.Title;
        ViewBag.CtaText = ctaPage.ContentHtml;

        return View(faculties);
    }

    // ================= PUBLIC-FACING PAGE: one faculty, its own page =================
    // GET: /Faculties/Detail/{slug} -> this faculty's OWN dedicated page
    // (hero + department card(s) + program modal), same shape as the
    // original static faculty-it.html mockup. Unlike Public() above -- which
    // lists every faculty together on one shared grid -- this route exists
    // so a nav item like "Faculty of IT & Cyber Security" opens straight
    // into that faculty's own page instead of landing on the shared list.
    [AllowAnonymous]
    public async Task<IActionResult> Detail(string id, string? edit = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.Equals("information-technology-computer-sciences", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(Detail), new { id = "information-technology-cyber-security", edit });
        }

        bool wantsEdit = !string.IsNullOrEmpty(edit) &&
            (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

        ApplicationUser? currentUser = null;
        bool hasFacultyPerm = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                hasFacultyPerm = await _permissionService.HasFacultyPermissionAsync(currentUser.Id, id)
                              || await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.FacultyAll)
                              || await _permissionService.IsSuperAdminAsync(currentUser.Id);
            }
        }

        bool canEdit = currentUser != null && (User.IsInRole("Admin") || hasFacultyPerm);
        ViewBag.CanEdit = canEdit;
        ViewBag.EditMode = wantsEdit && canEdit;

        ViewBag.CurrentUserName = "";
        if (currentUser != null)
        {
            ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (User.Identity?.Name ?? "");
        }

        await EnsureStandardFacultiesSeededAsync();

        var faculty = await _context.Faculties
            .Include(f => f.Programs).ThenInclude(p => p.ProgramCareers)
            .Include(f => f.Programs).ThenInclude(p => p.ProgramHighlights)
            .FirstOrDefaultAsync(f => f.Slug == id);

        if (faculty == null)
        {
            return NotFound();
        }

        // Load or seed dynamic About section feature cards for this faculty
        var featsSlug = $"faculty-feats-{faculty.Slug}";
        var featsPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == featsSlug);
        if (featsPage == null)
        {
            var defaultFeats = new[]
            {
                new { icon = "fas fa-chalkboard-teacher", text = "Industry-experienced faculty members" },
                new { icon = "fas fa-flask", text = "State-of-the-art labs and simulation centers" },
                new { icon = "fas fa-handshake", text = "Strong industry partnerships" },
                new { icon = "fas fa-globe", text = "Internationally recognized curricula" }
            };
            featsPage = new Page
            {
                Slug = featsSlug,
                Title = $"{faculty.Name} Features",
                ContentHtml = System.Text.Json.JsonSerializer.Serialize(defaultFeats),
                IsPublished = true
            };
            _context.Pages.Add(featsPage);
            await _context.SaveChangesAsync();
        }
        ViewBag.FacultyFeaturesJson = featsPage.ContentHtml;

        return View(faculty);
    }

    public class QuickSaveFacultyFeaturesRequest
    {
        public int FacultyId { get; set; }
        public string FeaturesJson { get; set; } = null!;
    }

    // POST: /Faculties/QuickSaveFacultyFeatures  (Admin only, Edit Mode only -- saves feature cards)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickSaveFacultyFeatures([FromBody] QuickSaveFacultyFeaturesRequest req)
    {
        if (req == null || req.FacultyId <= 0 || string.IsNullOrWhiteSpace(req.FeaturesJson))
        {
            return BadRequest(new { success = false, message = "Features required." });
        }

        var faculty = await _context.Faculties.FindAsync(req.FacultyId);
        if (faculty == null)
        {
            return NotFound(new { success = false, message = "Faculty not found." });
        }

        var featsSlug = $"faculty-feats-{faculty.Slug}";
        var featsPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == featsSlug);
        if (featsPage == null)
        {
            featsPage = new Page { Slug = featsSlug, Title = $"{faculty.Name} Features", IsPublished = true };
            _context.Pages.Add(featsPage);
        }

        featsPage.ContentHtml = req.FeaturesJson.Trim();
        featsPage.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return Json(new { success = true });
    }

    public class QuickAddFacultyRequest
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? ColorHex { get; set; }
        public string? Description { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddFaculty([FromBody] QuickAddFacultyRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Name))
        {
            return BadRequest(new { success = false, message = "Faculty name is required." });
        }

        var slugBase = Slugify(req.Name);
        var slug = slugBase;
        int suffix = 2;
        while (await _context.Faculties.AnyAsync(f => f.Slug == slug))
        {
            slug = slugBase + "-" + suffix;
            suffix++;
        }

        var faculty = new Faculty
        {
            Name = req.Name.Trim(),
            Slug = slug,
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? "fas fa-graduation-cap" : req.Icon.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(req.ColorHex) ? "#0b3a5c" : req.ColorHex.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description)
                ? $"{req.Name.Trim()} at National Aviation University, delivering industry-focused aviation education."
                : req.Description.Trim()
        };

        _context.Faculties.Add(faculty);
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            facultyId = faculty.FacultyId,
            slug = faculty.Slug,
            name = faculty.Name,
            category = DeriveCategory(faculty.Name),
            icon = faculty.Icon,
            colorHex = faculty.ColorHex,
            description = faculty.Description
        });
    }

    public class QuickUpdateFacultyRequest
    {
        public int FacultyId { get; set; }
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public string? ColorHex { get; set; }
        public string? Description { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateFaculty([FromBody] QuickUpdateFacultyRequest req)
    {
        if (req == null || req.FacultyId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid faculty." });
        }

        var faculty = await _context.Faculties.FindAsync(req.FacultyId);
        if (faculty == null)
        {
            return NotFound(new { success = false, message = "Faculty not found." });
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            faculty.Name = req.Name.Trim();
        }
        if (!string.IsNullOrWhiteSpace(req.Icon))
        {
            faculty.Icon = req.Icon.Trim();
        }
        if (!string.IsNullOrWhiteSpace(req.ColorHex))
        {
            faculty.ColorHex = req.ColorHex.Trim();
        }
        if (req.Description != null)
        {
            faculty.Description = req.Description.Trim();
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            facultyId = faculty.FacultyId,
            name = faculty.Name,
            category = DeriveCategory(faculty.Name),
            icon = faculty.Icon,
            colorHex = faculty.ColorHex,
            description = faculty.Description
        });
    }

    public class QuickDeleteFacultyRequest
    {
        public int FacultyId { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteFaculty([FromBody] QuickDeleteFacultyRequest req)
    {
        if (req == null || req.FacultyId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid faculty." });
        }

        var faculty = await _context.Faculties
            .Include(f => f.Programs)
            .FirstOrDefaultAsync(f => f.FacultyId == req.FacultyId);

        if (faculty == null)
        {
            return NotFound(new { success = false, message = "Faculty not found (it may already be deleted)." });
        }

        if (faculty.Programs.Count > 0)
        {
            return BadRequest(new
            {
                success = false,
                message = $"Can't delete \"{faculty.Name}\" -- it still has {faculty.Programs.Count} program(s). Delete those first."
            });
        }

        _context.Faculties.Remove(faculty);
        await _context.SaveChangesAsync();

        return Json(new { success = true, facultyId = req.FacultyId });
    }

    public class QuickDeleteProgramRequest
    {
        public int ProgramId { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteProgram([FromBody] QuickDeleteProgramRequest req)
    {
        if (req == null || req.ProgramId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid program." });
        }

        var program = await _context.Programs.FindAsync(req.ProgramId);
        if (program == null)
        {
            return NotFound(new { success = false, message = "Program not found (it may already be deleted)." });
        }

        _context.Programs.Remove(program);
        await _context.SaveChangesAsync();

        return Json(new { success = true, programId = req.ProgramId });
    }

    public class QuickDeleteDepartmentRequest
    {
        public int FacultyId { get; set; }
        public string? Department { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteDepartment([FromBody] QuickDeleteDepartmentRequest req)
    {
        if (req == null || req.FacultyId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid department request." });
        }

        var dept = (req.Department ?? string.Empty).Trim();
        var faculty = await _context.Faculties.FindAsync(req.FacultyId);
        if (faculty == null)
        {
            return NotFound(new { success = false, message = "Faculty not found." });
        }

        var programs = await _context.Programs
            .Where(p => p.FacultyId == req.FacultyId &&
                        (p.Department == dept ||
                         (string.IsNullOrEmpty(dept) && string.IsNullOrEmpty(p.Department)) ||
                         (dept == faculty.Name && string.IsNullOrEmpty(p.Department))))
            .ToListAsync();

        if (programs.Count > 0)
        {
            var programIds = programs.Select(p => p.ProgramId).ToList();

            var careers = await _context.ProgramCareers
                .Where(c => programIds.Contains(c.ProgramId))
                .ToListAsync();
            if (careers.Count > 0)
            {
                _context.ProgramCareers.RemoveRange(careers);
            }

            var highlights = await _context.ProgramHighlights
                .Where(h => programIds.Contains(h.ProgramId))
                .ToListAsync();
            if (highlights.Count > 0)
            {
                _context.ProgramHighlights.RemoveRange(highlights);
            }

            _context.Programs.RemoveRange(programs);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true, deletedProgramsCount = programs.Count });
    }

    public class QuickAddProgramCareerRequest
    {
        public int ProgramId { get; set; }
        public string? CareerTitle { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddProgramCareer([FromBody] QuickAddProgramCareerRequest req)
    {
        if (req == null || req.ProgramId <= 0 || string.IsNullOrWhiteSpace(req.CareerTitle))
        {
            return BadRequest(new { success = false, message = "Career title is required." });
        }

        var programExists = await _context.Programs.AnyAsync(p => p.ProgramId == req.ProgramId);
        if (!programExists)
        {
            return BadRequest(new { success = false, message = "Invalid program." });
        }

        var nextOrder = await _context.ProgramCareers
            .Where(c => c.ProgramId == req.ProgramId)
            .Select(c => c.DisplayOrder)
            .DefaultIfEmpty(-1)
            .MaxAsync() + 1;

        var career = new ProgramCareer
        {
            ProgramId = req.ProgramId,
            CareerTitle = req.CareerTitle.Trim(),
            DisplayOrder = nextOrder
        };
        _context.ProgramCareers.Add(career);
        await _context.SaveChangesAsync();

        return Json(new { success = true, careerId = career.ProgramCareerId, careerTitle = career.CareerTitle });
    }

    public class QuickDeleteProgramCareerRequest
    {
        public int CareerId { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteProgramCareer([FromBody] QuickDeleteProgramCareerRequest req)
    {
        if (req == null || req.CareerId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid career." });
        }

        var career = await _context.ProgramCareers.FindAsync(req.CareerId);
        if (career == null)
        {
            return NotFound(new { success = false, message = "Career not found (it may already be deleted)." });
        }

        _context.ProgramCareers.Remove(career);
        await _context.SaveChangesAsync();

        return Json(new { success = true, careerId = req.CareerId });
    }

    public class QuickAddProgramHighlightRequest
    {
        public int ProgramId { get; set; }
        public string? HighlightText { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickAddProgramHighlight([FromBody] QuickAddProgramHighlightRequest req)
    {
        if (req == null || req.ProgramId <= 0 || string.IsNullOrWhiteSpace(req.HighlightText))
        {
            return BadRequest(new { success = false, message = "Highlight text is required." });
        }

        var programExists = await _context.Programs.AnyAsync(p => p.ProgramId == req.ProgramId);
        if (!programExists)
        {
            return BadRequest(new { success = false, message = "Invalid program." });
        }

        var nextOrder = await _context.ProgramHighlights
            .Where(h => h.ProgramId == req.ProgramId)
            .Select(h => h.DisplayOrder)
            .DefaultIfEmpty(-1)
            .MaxAsync() + 1;

        var highlight = new ProgramHighlight
        {
            ProgramId = req.ProgramId,
            HighlightText = req.HighlightText.Trim(),
            DisplayOrder = nextOrder
        };
        _context.ProgramHighlights.Add(highlight);
        await _context.SaveChangesAsync();

        return Json(new { success = true, highlightId = highlight.ProgramHighlightId, highlightText = highlight.HighlightText });
    }

    public class QuickDeleteProgramHighlightRequest
    {
        public int HighlightId { get; set; }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteProgramHighlight([FromBody] QuickDeleteProgramHighlightRequest req)
    {
        if (req == null || req.HighlightId <= 0)
        {
            return BadRequest(new { success = false, message = "Invalid highlight." });
        }

        var highlight = await _context.ProgramHighlights.FindAsync(req.HighlightId);
        if (highlight == null)
        {
            return NotFound(new { success = false, message = "Highlight not found (it may already be deleted)." });
        }

        _context.ProgramHighlights.Remove(highlight);
        await _context.SaveChangesAsync();

        return Json(new { success = true, highlightId = req.HighlightId });
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

        const string ctaSlug = "faculties-public-cta";
        var ctaPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == ctaSlug);
        if (ctaPage == null)
        {
            ctaPage = new Page { Slug = ctaSlug, Title = req.Title.Trim(), IsPublished = true };
            _context.Pages.Add(ctaPage);
        }

        ctaPage.Title = req.Title.Trim();
        ctaPage.ContentHtml = (req.Text ?? string.Empty).Trim();
        ctaPage.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Json(new { success = true, title = ctaPage.Title, text = ctaPage.ContentHtml });
    }

    // The card's small "eyebrow" category label (and the filter-bar buttons)
    // are derived from the faculty name itself -- e.g. "Faculty of
    // Engineering" -> "Engineering" -- so admins only ever need to type one
    // name when adding a faculty, same as how Schools work on the Short
    // Courses page.
    public static string DeriveCategory(string? name)
    {
        var n = (name ?? string.Empty).Trim();
        foreach (var prefix in new[] { "Faculty of ", "School of ", "Department of ", "College of " })
        {
            if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return n.Substring(prefix.Length).Trim();
            }
        }
        return n;
    }

    private static string Slugify(string text)
    {
        var slug = (text ?? string.Empty).Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        bool lastWasDash = false;
        foreach (var ch in slug)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }
        return sb.ToString().Trim('-');
    }

    // =============================================================================

    // GET: FACULTYS
    public async Task<IActionResult> Index()
    {
        return View(await _context.Faculties.ToListAsync());
    }

    // GET: FACULTYS/Details/5
    public async Task<IActionResult> Details(int? facultyid)
    {
        if (facultyid == null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(m => m.FacultyId == facultyid);
        if (faculty == null)
        {
            return NotFound();
        }

        return View(faculty);
    }

    // GET: FACULTYS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: FACULTYS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FacultyId,Slug,Name,Icon,ColorHex,Description,Programs")] Faculty faculty)
    {
        if (ModelState.IsValid)
        {
            _context.Add(faculty);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(faculty);
    }

    // GET: FACULTYS/Edit/5
    public async Task<IActionResult> Edit(int? facultyid)
    {
        if (facultyid == null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties.FindAsync(facultyid);
        if (faculty == null)
        {
            return NotFound();
        }
        return View(faculty);
    }

    // POST: FACULTYS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? facultyid, [Bind("FacultyId,Slug,Name,Icon,ColorHex,Description,Programs")] Faculty faculty)
    {
        if (facultyid != faculty.FacultyId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(faculty);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacultyExists(faculty.FacultyId))
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
        return View(faculty);
    }

    // GET: FACULTYS/Delete/5
    public async Task<IActionResult> Delete(int? facultyid)
    {
        if (facultyid == null)
        {
            return NotFound();
        }

        var faculty = await _context.Faculties
            .FirstOrDefaultAsync(m => m.FacultyId == facultyid);
        if (faculty == null)
        {
            return NotFound();
        }

        return View(faculty);
    }

    // POST: FACULTYS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? facultyid)
    {
        var faculty = await _context.Faculties.FindAsync(facultyid);
        if (faculty != null)
        {
            _context.Faculties.Remove(faculty);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    public async Task<IActionResult> AddStat(int facilityId, string statNumber, string statLabel)
    {
        _context.FacilityStats.Add(new FacilityStat { FacilityId = facilityId, StatNumber = statNumber, StatLabel = statLabel });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStat(int id, int facilityId)
    {
        var row = await _context.FacilityStats.FindAsync(id);
        if (row != null) { _context.FacilityStats.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }

    [HttpPost]
    public async Task<IActionResult> AddService(int facilityId, string icon, string title, string description)
    {
        _context.FacilityServices.Add(new FacilityService { FacilityId = facilityId, Icon = icon, Title = title, Description = description });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteService(int id, int facilityId)
    {
        var row = await _context.FacilityServices.FindAsync(id);
        if (row != null) { _context.FacilityServices.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }

    [HttpPost]
    public async Task<IActionResult> AddHour(int facilityId, string dayOrLabel, string timeValue)
    {
        _context.FacilityHours.Add(new FacilityHour { FacilityId = facilityId, DayOrLabel = dayOrLabel, TimeValue = timeValue });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteHour(int id, int facilityId)
    {
        var row = await _context.FacilityHours.FindAsync(id);
        if (row != null) { _context.FacilityHours.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageContent), new { id = facilityId });
    }
    public async Task<IActionResult> ManageContent(int? id)
    {
        if (id == null) return NotFound();
        var faculty = await _context.Faculties
            .Include(f => f.Programs)
            .FirstOrDefaultAsync(f => f.FacultyId == id);
        if (faculty == null) return NotFound();
        return View(faculty);
    }

    [HttpPost]
    public async Task<IActionResult> AddProgram(int facultyId, string code, string name, string? department, string? icon, string? colorHex, string? overview, string? undergradYears, string? adpyears, string? gradYears)
    {
        _context.Programs.Add(new NAU_Web.Models.Program
        {
            FacultyId = facultyId,
            Code = code,
            Name = name,
            Department = department,
            Icon = icon,
            ColorHex = colorHex,
            Overview = overview,
            UndergradYears = undergradYears,
            Adpyears = adpyears,
            GradYears = gradYears
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageContent), new { id = facultyId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProgram(int id, int facultyId)
    {
        var row = await _context.Programs.FindAsync(id);
        if (row != null) { _context.Programs.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageContent), new { id = facultyId });
    }

    public async Task<IActionResult> ManageProgram(int? id)
    {
        if (id == null) return NotFound();
        var program = await _context.Programs
            .Include(p => p.ProgramCareers)
            .Include(p => p.ProgramHighlights)
            .Include(p => p.Faculty)
            .FirstOrDefaultAsync(p => p.ProgramId == id);
        if (program == null) return NotFound();
        return View(program);
    }

    [HttpPost]
    public async Task<IActionResult> AddCareer(int programId, string careerTitle)
    {
        _context.ProgramCareers.Add(new ProgramCareer { ProgramId = programId, CareerTitle = careerTitle });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageProgram), new { id = programId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCareer(int id, int programId)
    {
        var row = await _context.ProgramCareers.FindAsync(id);
        if (row != null) { _context.ProgramCareers.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageProgram), new { id = programId });
    }

    [HttpPost]
    public async Task<IActionResult> AddHighlight(int programId, string highlightText)
    {
        _context.ProgramHighlights.Add(new ProgramHighlight { ProgramId = programId, HighlightText = highlightText });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(ManageProgram), new { id = programId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteHighlight(int id, int programId)
    {
        var row = await _context.ProgramHighlights.FindAsync(id);
        if (row != null) { _context.ProgramHighlights.Remove(row); await _context.SaveChangesAsync(); }
        return RedirectToAction(nameof(ManageProgram), new { id = programId });
    }

    private async Task EnsureStandardFacultiesSeededAsync()
    {
        var standardFaculties = new[]
        {
            new {
                Slug = "engineering",
                Name = "Faculty of Engineering",
                Icon = "fas fa-cogs",
                ColorHex = "#1a4a7a",
                Description = "Pioneering excellence in aviation systems and infrastructure engineering, training the builders and innovators of tomorrow's aviation world.",
                Programs = new[]
                {
                    new {
                        Code = "ANS",
                        Name = "ANS Engineering",
                        Department = "Dept. of Aviation System & Engineering",
                        Icon = "fas fa-tower-cell",
                        ColorHex = "#1a4a7a",
                        Overview = "The ANS Engineering program covers Air Navigation Systems design, maintenance, and operation. Students gain expertise in radar systems, communication technologies, and navigation aids critical to modern aviation.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "ATC Engineer", "CNS/ATM Specialist", "Navigation Systems Designer", "Avionics Engineer", "Systems Integration Specialist" },
                        Highlights = new[] { "Simulation labs with real ATC equipment", "Industry placements with CAA and international aviation bodies", "Covers ICAO CNS/ATM frameworks", "Access to state-of-the-art radar and navigation system facilities", "Graduates eligible for CAA technical licensing" }
                    },
                    new {
                        Code = "AIRPORTENG",
                        Name = "Airport Engineering",
                        Department = "Dept. of Aviation System & Engineering",
                        Icon = "fas fa-building",
                        ColorHex = "#1a4a7a",
                        Overview = "Airport Engineering trains professionals in the planning, design, construction, and maintenance of airport infrastructure including runways, terminals, taxiways, and lighting systems in compliance with ICAO Annex 14.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Airport Civil Engineer", "Infrastructure Planner", "Airfield Designer", "Pavement Engineer", "Terminal Development Manager" },
                        Highlights = new[] { "ICAO Annex 14 compliant curriculum", "Field visits to major international airports", "Runway and pavement engineering labs", "Covers airfield lighting, drainage, and traffic systems", "Collaboration with Pakistan Civil Aviation Authority" }
                    }
                }
            },
            new {
                Slug = "management",
                Name = "Faculty of Management Sciences",
                Icon = "fas fa-chart-line",
                ColorHex = "#1e6b3e",
                Description = "Developing strategic leaders and aviation managers equipped with the business acumen to drive growth across the global aviation industry.",
                Programs = new[]
                {
                    new {
                        Code = "AVMGMT",
                        Name = "Aviation Management",
                        Department = "Department of Aviation Management",
                        Icon = "fas fa-chart-pie",
                        ColorHex = "#1e6b3e",
                        Overview = "Focuses on the business, economics, and management dimensions of the aviation industry. Combines aviation-specific knowledge with modern management principles to develop well-rounded leaders.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Aviation Manager", "Airline Operations Controller", "Airport Terminal Manager", "Aviation Commercial Analyst" },
                        Highlights = new[] { "Curriculum aligned with IATA & ICAO management guidelines", "Case studies on global airline business models", "Corporate internship placement program" }
                    },
                    new {
                        Code = "MBA",
                        Name = "MBA (Aviation)",
                        Department = "Department of Aviation Management",
                        Icon = "fas fa-graduation-cap",
                        ColorHex = "#1e6b3e",
                        Overview = "An advanced business administration program designed for mid-level professionals seeking senior leadership and strategic positions in the aviation sector.",
                        Undergrad = "N/A", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Senior Aviation Executive", "Airline General Manager", "Aviation Strategy Consultant" },
                        Highlights = new[] { "Flexible evening/weekend schedule for working professionals", "Focus on aviation finance, law, and fleet planning" }
                    }
                }
            },
            new {
                Slug = "operations",
                Name = "Faculty of Aviation Operations",
                Icon = "fas fa-plane-departure",
                ColorHex = "#14557a",
                Description = "Delivering world-class operational training in air traffic management, flight ops, and airport ground services.",
                Programs = new[]
                {
                    new {
                        Code = "ATM",
                        Name = "Air Traffic Management",
                        Department = "Dept. of Aviation Operations",
                        Icon = "fas fa-tower-broadcast",
                        ColorHex = "#14557a",
                        Overview = "The Air Traffic Management program prepares graduates to become certified air traffic controllers and ATM systems managers. The curriculum covers ATC procedures, flight data management, radar surveillance, and NextGen/SESAR ATM concepts.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Air Traffic Controller", "ATM Systems Manager", "Flight Data Officer", "ATC Instructor", "ATM Consultant" },
                        Highlights = new[] { "ICAO-standard ATC simulation training", "Practical training at Hyderabad and major Pakistani airports", "CAAB-aligned licensing pathways", "Covers both procedural and radar control", "International exchange programs with aviation academies" }
                    },
                    new {
                        Code = "AIRPORTOPS",
                        Name = "Airport Operations",
                        Department = "Dept. of Aviation Operations",
                        Icon = "fas fa-plane-circle-check",
                        ColorHex = "#14557a",
                        Overview = "Airport Operations trains students in the day-to-day management of airport facilities, ground handling, passenger services, airside operations, and safety compliance. Graduates are prepared for leadership roles in airport management organizations.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Airport Operations Manager", "Ground Handling Supervisor", "Airside Safety Officer", "Passenger Services Manager", "Terminal Operations Coordinator" },
                        Highlights = new[] { "Hands-on training at operational airports", "Covers ICAO Annex 14 and safety management systems", "Airside vehicle and equipment operation certification", "Industry internships with CAA and private airport operators", "Emergency planning and crisis management modules" }
                    }
                }
            },
            new {
                Slug = "information-technology-cyber-security",
                Name = "Faculty of Information Technology & Cyber Security",
                Icon = "fas fa-laptop-code",
                ColorHex = "#1a3a5c",
                Description = "Equipping students with cutting-edge skills in aviation cyber security, digital systems, and information technology for a connected skies era.",
                Programs = new[]
                {
                    new {
                        Code = "AVCS",
                        Name = "Aviation Cyber Security",
                        Department = "Dept. of Aviation Cyber Security",
                        Icon = "fas fa-shield-halved",
                        ColorHex = "#1a3a5c",
                        Overview = "The Aviation Cyber Security program is specifically designed to address the unique digital security challenges facing modern aviation systems. Students gain expertise in securing ATC systems, aircraft communication networks, and airport digital infrastructure.",
                        Undergrad = "4 Years", ADP = "2 Years", Grad = "2 Years",
                        Careers = new[] { "Aviation Cyber Analyst", "Network Security Engineer", "Digital Forensics Specialist", "IT Security Auditor", "SIEM Operations Specialist" },
                        Highlights = new[] { "Specialized aviation cybersecurity curriculum — only one of its kind in Pakistan", "Cyber range and penetration testing labs", "ICAO Cybersecurity Framework integration", "Collaboration with national CERT and CAA", "Covers ATM, ATC and airport system vulnerability management" }
                    }
                }
            }
        };

        // Migrate legacy slug/name if present in database
        var legacyFac = await _context.Faculties.FirstOrDefaultAsync(f => f.Slug == "information-technology-computer-sciences");
        if (legacyFac != null)
        {
            legacyFac.Slug = "information-technology-cyber-security";
            legacyFac.Name = "Faculty of Information Technology & Cyber Security";
            await _context.SaveChangesAsync();
        }
        var legacyFeats = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == "faculty-feats-information-technology-computer-sciences");
        if (legacyFeats != null)
        {
            legacyFeats.Slug = "faculty-feats-information-technology-cyber-security";
            legacyFeats.Title = "Faculty of Information Technology & Cyber Security Features";
            await _context.SaveChangesAsync();
        }

        foreach (var item in standardFaculties)
        {
            var fac = await _context.Faculties.FirstOrDefaultAsync(f => f.Slug == item.Slug);
            if (fac == null)
            {
                fac = new Faculty
                {
                    Slug = item.Slug,
                    Name = item.Name,
                    Icon = item.Icon,
                    ColorHex = item.ColorHex,
                    Description = item.Description
                };
                _context.Faculties.Add(fac);
                await _context.SaveChangesAsync();
            }
            else
            {
                fac.Name = item.Name;
                fac.Icon = item.Icon;
                fac.ColorHex = item.ColorHex;
                fac.Description = item.Description;
                await _context.SaveChangesAsync();
            }

            foreach (var p in item.Programs)
            {
                var prog = await _context.Programs.FirstOrDefaultAsync(pr => pr.FacultyId == fac.FacultyId && pr.Code == p.Code);
                if (prog == null)
                {
                    prog = new NAU_Web.Models.Program
                    {
                        FacultyId = fac.FacultyId,
                        Code = p.Code,
                        Name = p.Name,
                        Department = p.Department,
                        Icon = p.Icon,
                        ColorHex = p.ColorHex,
                        Overview = p.Overview,
                        UndergradYears = p.Undergrad,
                        Adpyears = p.ADP,
                        GradYears = p.Grad
                    };
                    _context.Programs.Add(prog);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    prog.Name = p.Name;
                    prog.Department = p.Department;
                    prog.Icon = p.Icon;
                    prog.ColorHex = p.ColorHex;
                    prog.Overview = p.Overview;
                    prog.UndergradYears = p.Undergrad;
                    prog.Adpyears = p.ADP;
                    prog.GradYears = p.Grad;
                    await _context.SaveChangesAsync();
                }

                if (!await _context.ProgramCareers.AnyAsync(c => c.ProgramId == prog.ProgramId))
                {
                    for (int i = 0; i < p.Careers.Length; i++)
                    {
                        _context.ProgramCareers.Add(new ProgramCareer { ProgramId = prog.ProgramId, CareerTitle = p.Careers[i], DisplayOrder = i });
                    }
                    await _context.SaveChangesAsync();
                }

                if (!await _context.ProgramHighlights.AnyAsync(h => h.ProgramId == prog.ProgramId))
                {
                    for (int i = 0; i < p.Highlights.Length; i++)
                    {
                        _context.ProgramHighlights.Add(new ProgramHighlight { ProgramId = prog.ProgramId, HighlightText = p.Highlights[i], DisplayOrder = i });
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }
    }

    private bool FacultyExists(int? facultyid)
    {
        return _context.Faculties.Any(e => e.FacultyId == facultyid);
    }
}

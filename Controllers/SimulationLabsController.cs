using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

public class SimulationLabsController : Controller
{
    private readonly NauDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPermissionService _permissionService;

    private const string FlightSectionKey = "flight-simulators";
    private const string AtcSectionKey = "atc-simulator";
    private const string AvionicsSectionKey = "avionics-lab";

    public SimulationLabsController(
        NauDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserPermissionService permissionService)
    {
        _context = context;
        _userManager = userManager;
        _permissionService = permissionService;
    }

    // Same pattern used on every other public page (Facilities, Faculties,
    // About, ...) so the shared header shows "Hi, <name>" / Admin Panel
    // correctly for a logged-in user.
    private async Task SetCurrentUserNameAsync()
    {
        ViewBag.CurrentUserName = "";
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.CurrentUserName = currentUser != null && !string.IsNullOrWhiteSpace(currentUser.FullName)
                ? currentUser.FullName
                : (User.Identity.Name ?? "");
        }
    }

    // Loads the "flight-simulators" section (with its images & feature chips),
    // creating it on first run with the same content the page used to show
    // as static HTML, so the page never appears empty.
    private async Task<SimulationLabSection> GetOrCreateFlightSectionAsync()
    {
        var section = await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .FirstOrDefaultAsync(s => s.SectionKey == FlightSectionKey);

        if (section != null) return section;

        section = new SimulationLabSection
        {
            SectionKey = FlightSectionKey,
            Tag = "Simulation & Labs — 01",
            Title = "Flight Simulators",
            Description = "State-of-the-art Full Flight Simulators and Flight Training Devices used to train pilots on realistic flight scenarios, emergency procedures and multi-aircraft type ratings.",
            IconClass = "fas fa-plane",
            IsAvailable = true,
            DisplayOrder = 0
        };
        _context.SimulationLabSections.Add(section);
        await _context.SaveChangesAsync();

        _context.SimulationLabImages.AddRange(
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Full Flight Simulator (FFS)", Badge = "Level D Certified", IconClass = "fas fa-plane", IsLarge = true, DisplayOrder = 0 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Flight Training Device (FTD)", IconClass = "fas fa-plane-departure", IsLarge = false, DisplayOrder = 1 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Instructor Operating Station", IconClass = "fas fa-gauge-high", IsLarge = false, DisplayOrder = 2 }
        );
        _context.SimulationLabFeatures.AddRange(
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-check-circle", Label = "CAA Certified", DisplayOrder = 0 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-layer-group", Label = "Level D Simulator", DisplayOrder = 1 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-plane-departure", Label = "Multi-Aircraft Type", DisplayOrder = 2 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-arrows-spin", Label = "Full Motion Platform", DisplayOrder = 3 }
        );
        await _context.SaveChangesAsync();

        return await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .FirstAsync(s => s.SimulationLabSectionId == section.SimulationLabSectionId);
    }

    // Loads the "atc-simulator" section (with its images & feature chips),
    // creating it on first run with the same content the page used to show
    // as static HTML, so the page never appears empty. Same pattern as
    // GetOrCreateFlightSectionAsync above.
    private async Task<SimulationLabSection> GetOrCreateAtcSectionAsync()
    {
        var section = await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c!.SimulationLabInfoItems)
            .FirstOrDefaultAsync(s => s.SectionKey == AtcSectionKey);

        if (section != null)
        {
            await EnsureAtcInfoCardAsync(section);
            return section;
        }

        section = new SimulationLabSection
        {
            SectionKey = AtcSectionKey,
            Tag = "Simulation & Labs — 02",
            Title = "ATC Simulator",
            Description = "Advanced Air Traffic Control simulation suite featuring live radar display, voice communication systems, and multi-sector scenario training — preparing controllers for real airspace management.",
            IconClass = "fas fa-tower-broadcast",
            IsAvailable = true,
            DisplayOrder = 1
        };
        _context.SimulationLabSections.Add(section);
        await _context.SaveChangesAsync();

        _context.SimulationLabImages.AddRange(
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Radar Control Room — Live Simulation", Badge = "Primary ATC Suite", IconClass = "fas fa-circle-dot", IsLarge = false, DisplayOrder = 0 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Voice Communication & Strip Bay", Badge = "Approach Control", IconClass = "fas fa-headset", IsLarge = false, DisplayOrder = 1 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Panoramic Aerodrome Control Tower View", Badge = "Tower Simulator — Hyderabad FIR", IconClass = "fas fa-binoculars", IsLarge = true, DisplayOrder = 0 }
        );
        _context.SimulationLabFeatures.AddRange(
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-check-circle", Label = "CAA Endorsed", DisplayOrder = 0 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-layer-group", Label = "Multi-Sector", DisplayOrder = 1 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-microphone", Label = "Live Voice Comms", DisplayOrder = 2 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-exclamation-triangle", Label = "Emergency Scenarios", DisplayOrder = 3 }
        );
        await _context.SaveChangesAsync();
        await EnsureAtcInfoCardAsync(section);

        return await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c!.SimulationLabInfoItems)
            .FirstAsync(s => s.SimulationLabSectionId == section.SimulationLabSectionId);
    }

    // Seeds the ATC section's sidebar "info card" (Simulator Capabilities)
    // the first time it's missing -- covers both brand-new sections and
    // existing sections created before this feature existed, so the page
    // keeps showing the same content it always has, but now DB-driven and
    // editable. Runs on every load but only inserts once (no-op after that).
    private async Task EnsureAtcInfoCardAsync(SimulationLabSection section)
    {
        if (section.SimulationLabInfoCard != null) return;

        var card = new SimulationLabInfoCard
        {
            SimulationLabSectionId = section.SimulationLabSectionId,
            Heading = "Simulator Capabilities",
            HighlightText = "ICAO Doc 9868 Compliant Training",
            HighlightIcon = "fas fa-star",
            HighlightColor = "purple"
        };
        _context.SimulationLabInfoCards.Add(card);
        await _context.SaveChangesAsync();

        _context.SimulationLabInfoItems.AddRange(
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "Multi-sector en-route, approach and aerodrome control", ColorHex = "var(--atc-a)", DisplayOrder = 0 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "STRIP & ATIS integration", ColorHex = "var(--atc-a)", DisplayOrder = 1 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "Emergency scenario training", ColorHex = "var(--atc-a)", DisplayOrder = 2 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "Instructor Operating Station (IOS)", ColorHex = "var(--atc-a)", DisplayOrder = 3 }
        );
        await _context.SaveChangesAsync();

        section.SimulationLabInfoCard = card;
    }

    // Loads the "avionics-lab" section (with its images & feature chips),
    // creating it on first run with the same content the page used to show
    // as static HTML, so the page never appears empty. Same pattern as
    // GetOrCreateAtcSectionAsync above.
    private async Task<SimulationLabSection> GetOrCreateAvionicsSectionAsync()
    {
        var section = await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c!.SimulationLabInfoItems)
            .FirstOrDefaultAsync(s => s.SectionKey == AvionicsSectionKey);

        if (section != null)
        {
            await EnsureAvionicsInfoCardAsync(section);
            return section;
        }

        section = new SimulationLabSection
        {
            SectionKey = AvionicsSectionKey,
            Tag = "Simulation & Labs — 03",
            Title = "Avionics Lab",
            Description = "Fully equipped avionics training laboratory with operational navigation, communication and instrumentation rigs — bridging theoretical knowledge with practical hands-on systems training.",
            IconClass = "fas fa-microchip",
            IsAvailable = true,
            DisplayOrder = 2
        };
        _context.SimulationLabSections.Add(section);
        await _context.SaveChangesAsync();

        _context.SimulationLabImages.AddRange(
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Navigation Systems — Training Rig", Badge = "VOR / ILS / GPS", IconClass = "fas fa-satellite-dish", IsLarge = false, DisplayOrder = 0 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Radio Communication — Systems", Badge = "VHF / HF / SELCAL", IconClass = "fas fa-broadcast-tower", IsLarge = false, DisplayOrder = 1 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Flight Instruments — Bench", Badge = "ADI / HSI / EFIS", IconClass = "fas fa-tachometer-alt", IsLarge = false, DisplayOrder = 2 },
            new SimulationLabImage { SimulationLabSectionId = section.SimulationLabSectionId, Title = "Avionics Maintenance Workstation", Badge = "Electrical Systems Lab", IconClass = "fas fa-tools", IsLarge = true, DisplayOrder = 0 }
        );
        _context.SimulationLabFeatures.AddRange(
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-check-circle", Label = "CAA Certified", DisplayOrder = 0 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-book", Label = "EASA / PCAA Syllabus", DisplayOrder = 1 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-wrench", Label = "Live Equipment", DisplayOrder = 2 },
            new SimulationLabFeature { SimulationLabSectionId = section.SimulationLabSectionId, IconClass = "fas fa-user-graduate", Label = "Expert Instructors", DisplayOrder = 3 }
        );
        await _context.SaveChangesAsync();
        await EnsureAvionicsInfoCardAsync(section);

        return await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c!.SimulationLabInfoItems)
            .FirstAsync(s => s.SimulationLabSectionId == section.SimulationLabSectionId);
    }

    // Seeds the Avionics section's sidebar "info card" (Lab Equipment) the
    // first time it's missing. Same reasoning as EnsureAtcInfoCardAsync above.
    private async Task EnsureAvionicsInfoCardAsync(SimulationLabSection section)
    {
        if (section.SimulationLabInfoCard != null) return;

        var card = new SimulationLabInfoCard
        {
            SimulationLabSectionId = section.SimulationLabSectionId,
            Heading = "Lab Equipment",
            HighlightText = "Hands-on Practical Focus",
            HighlightIcon = "fas fa-tools",
            HighlightColor = "amber"
        };
        _context.SimulationLabInfoCards.Add(card);
        await _context.SaveChangesAsync();

        _context.SimulationLabInfoItems.AddRange(
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "Autopilot & FMS trainers", ColorHex = "var(--avionics-a)", DisplayOrder = 0 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "Weather radar systems", ColorHex = "var(--avionics-a)", DisplayOrder = 1 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "TCAS / ACAS rigs", ColorHex = "var(--avionics-a)", DisplayOrder = 2 },
            new SimulationLabInfoItem { SimulationLabInfoCardId = card.SimulationLabInfoCardId, Text = "FDR / CVR demonstration units", ColorHex = "var(--avionics-a)", DisplayOrder = 3 }
        );
        await _context.SaveChangesAsync();

        section.SimulationLabInfoCard = card;
    }

    // ================= PUBLIC-FACING PAGE (no login required) =================
    // GET: /SimulationLabs/Public  -> the "Simulation & Labs" page.
    // The first three sections (Flight Simulators, ATC Simulator, Avionics
    // Lab) are DB-driven and editable inline by admins via ?edit=true (same
    // convention as Faculties / News & Media / Short Courses).
    //
    // Any further sections (a 4th, 5th, ...) an admin creates from the
    // Sections admin panel (/SimulationLabs/Index -> "+ Add New Section")
    // are picked up automatically here via ExtraSections below and rendered
    // generically at the bottom of the page -- no code change needed to
    // show a newly added section.
    [AllowAnonymous]
    public async Task<IActionResult> Public(string? edit = null)
    {
        bool wantsEdit = !string.IsNullOrEmpty(edit) &&
            (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

        ApplicationUser? currentUser = null;
        bool hasLabPerm = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                hasLabPerm = await _permissionService.HasSimLabPermissionAsync(currentUser.Id, "sim-labs");
            }
        }

        bool isAdmin = currentUser != null && (User.IsInRole("Admin") || hasLabPerm);
        ViewBag.EditMode = wantsEdit && isAdmin;
        ViewBag.IsAdmin = isAdmin;

        await SetCurrentUserNameAsync();

        var flightSection = await GetOrCreateFlightSectionAsync();
        var atcSection = await GetOrCreateAtcSectionAsync();
        var avionicsSection = await GetOrCreateAvionicsSectionAsync();
        ViewBag.AtcSection = atcSection;
        ViewBag.AvionicsSection = avionicsSection;

        // Any section beyond the original 3 built-in ones (created by an
        // admin via the Sections admin panel). Hidden ones (IsAvailable ==
        // false) are only shown to admins in edit mode so they can preview
        // before publishing.
        var knownKeys = new[] { FlightSectionKey, AtcSectionKey, AvionicsSectionKey };
        var extraSectionsQuery = _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c!.SimulationLabInfoItems)
            .Where(s => !knownKeys.Contains(s.SectionKey));

        if (!(isAdmin && wantsEdit))
        {
            extraSectionsQuery = extraSectionsQuery.Where(s => s.IsAvailable);
        }

        var extraSections = await extraSectionsQuery
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SimulationLabSectionId)
            .ToListAsync();
        ViewBag.ExtraSections = extraSections;

        return View(flightSection);
    }

    // ================= ADMIN: SECTIONS LIST / CREATE / EDIT / DELETE =================
    // This is the admin panel for the Simulation & Labs page's sections.
    // It's what lets an admin add a brand-new 4th (5th, 6th, ...) section
    // -- e.g. "Cabin Crew Trainer" or "Ground Handling Lab" -- without any
    // code change. Every section created here is a normal row in the
    // SimulationLabSections table, so it's saved to the database exactly
    // like the original 3 sections, and shows up automatically on the
    // public page (see ExtraSections in the Public action above).
    // Same scaffolded CRUD convention used by FacilitiesController.

    // GET: /SimulationLabs/Index -> list of all sections (built-in + admin-added)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var sections = await _context.SimulationLabSections
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.SimulationLabSectionId)
            .ToListAsync();
        return View(sections);
    }

    // GET: /SimulationLabs/Create
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        var nextOrder = await _context.SimulationLabSections.CountAsync();
        // Default a brand-new section to "visible on the public page" so
        // admins don't accidentally publish a hidden section by forgetting
        // to tick the checkbox, and pre-fill a sensible display order.
        var section = new SimulationLabSection { IsAvailable = true, DisplayOrder = nextOrder };
        return View(section);
    }

    // POST: /SimulationLabs/Create
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SectionKey,Title,Tag,Description,IconClass,ColorHex,ColorHex2,IsAvailable,DisplayOrder")] SimulationLabSection section)
    {
        if (string.IsNullOrWhiteSpace(section.SectionKey))
        {
            ModelState.AddModelError(nameof(SimulationLabSection.SectionKey), "Section key is required.");
        }
        else if (await _context.SimulationLabSections.AnyAsync(s => s.SectionKey == section.SectionKey))
        {
            ModelState.AddModelError(nameof(SimulationLabSection.SectionKey), "This section key is already used by another section. Choose a unique one.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(section);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(section);
    }

    // GET: /SimulationLabs/Edit/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var section = await _context.SimulationLabSections.FindAsync(id);
        if (section == null) return NotFound();

        return View(section);
    }

    // POST: /SimulationLabs/Edit/5
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("SimulationLabSectionId,SectionKey,Title,Tag,Description,IconClass,ColorHex,ColorHex2,IsAvailable,DisplayOrder")] SimulationLabSection section)
    {
        if (id != section.SimulationLabSectionId) return NotFound();

        if (string.IsNullOrWhiteSpace(section.SectionKey))
        {
            ModelState.AddModelError(nameof(SimulationLabSection.SectionKey), "Section key is required.");
        }
        else if (await _context.SimulationLabSections.AnyAsync(s => s.SectionKey == section.SectionKey && s.SimulationLabSectionId != section.SimulationLabSectionId))
        {
            ModelState.AddModelError(nameof(SimulationLabSection.SectionKey), "This section key is already used by another section. Choose a unique one.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(section);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.SimulationLabSections.AnyAsync(s => s.SimulationLabSectionId == section.SimulationLabSectionId))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(section);
    }

    // GET: /SimulationLabs/Delete/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var section = await _context.SimulationLabSections.FirstOrDefaultAsync(s => s.SimulationLabSectionId == id);
        if (section == null) return NotFound();

        return View(section);
    }

    // POST: /SimulationLabs/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var section = await _context.SimulationLabSections.FindAsync(id);
        if (section != null)
        {
            _context.SimulationLabSections.Remove(section);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: /SimulationLabs/ToggleAvailable/5 -> quick show/hide from the admin list
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailable(int id)
    {
        var section = await _context.SimulationLabSections.FindAsync(id);
        if (section != null)
        {
            section.IsAvailable = !section.IsAvailable;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // ================= ADMIN: MANAGE IMAGES / FEATURES (any section) =================
    // Simple form-based editor for a section's image tiles and feature
    // chips -- works for ANY section, including new ones an admin creates,
    // so a 4th section gets the same content-management ability as the
    // original 3 without needing the bespoke modal/JS editor those have.

    // GET: /SimulationLabs/ManageContent/5
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManageContent(int? id)
    {
        if (id == null) return NotFound();

        var section = await _context.SimulationLabSections
            .Include(s => s.SimulationLabImages)
            .Include(s => s.SimulationLabFeatures)
            .FirstOrDefaultAsync(s => s.SimulationLabSectionId == id);

        if (section == null) return NotFound();
        return View(section);
    }

    // POST: /SimulationLabs/AddImage
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(int sectionId, string? title, string? badge, string? iconClass, string? imageUrl, bool isLarge = false)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            if (isLarge)
            {
                // Only one large tile per section -- replace it if one already exists.
                var existingLarge = await _context.SimulationLabImages
                    .FirstOrDefaultAsync(i => i.SimulationLabSectionId == sectionId && i.IsLarge);
                if (existingLarge != null)
                {
                    existingLarge.Title = title.Trim();
                    existingLarge.Badge = string.IsNullOrWhiteSpace(badge) ? null : badge.Trim();
                    existingLarge.IconClass = string.IsNullOrWhiteSpace(iconClass) ? null : iconClass.Trim();
                    existingLarge.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
                }
                else
                {
                    _context.SimulationLabImages.Add(new SimulationLabImage
                    {
                        SimulationLabSectionId = sectionId,
                        IsLarge = true,
                        Title = title.Trim(),
                        Badge = string.IsNullOrWhiteSpace(badge) ? null : badge.Trim(),
                        IconClass = string.IsNullOrWhiteSpace(iconClass) ? null : iconClass.Trim(),
                        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                        DisplayOrder = 0
                    });
                }
            }
            else
            {
                var nextOrder = await _context.SimulationLabImages.CountAsync(i => i.SimulationLabSectionId == sectionId && !i.IsLarge) + 1;
                _context.SimulationLabImages.Add(new SimulationLabImage
                {
                    SimulationLabSectionId = sectionId,
                    IsLarge = false,
                    Title = title.Trim(),
                    Badge = string.IsNullOrWhiteSpace(badge) ? null : badge.Trim(),
                    IconClass = string.IsNullOrWhiteSpace(iconClass) ? null : iconClass.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                    DisplayOrder = nextOrder
                });
            }
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageContent), new { id = sectionId });
    }

    // POST: /SimulationLabs/DeleteImage
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int id, int sectionId)
    {
        var img = await _context.SimulationLabImages.FindAsync(id);
        if (img != null && !img.IsLarge)
        {
            _context.SimulationLabImages.Remove(img);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageContent), new { id = sectionId });
    }

    // POST: /SimulationLabs/AddFeature
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFeature(int sectionId, string? iconClass, string? label)
    {
        if (!string.IsNullOrWhiteSpace(label))
        {
            var nextOrder = await _context.SimulationLabFeatures.CountAsync(f => f.SimulationLabSectionId == sectionId) + 1;
            _context.SimulationLabFeatures.Add(new SimulationLabFeature
            {
                SimulationLabSectionId = sectionId,
                Label = label.Trim(),
                IconClass = string.IsNullOrWhiteSpace(iconClass) ? "fas fa-check-circle" : iconClass.Trim(),
                DisplayOrder = nextOrder
            });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageContent), new { id = sectionId });
    }

    // POST: /SimulationLabs/DeleteFeature
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFeature(int id, int sectionId)
    {
        var f = await _context.SimulationLabFeatures.FindAsync(id);
        if (f != null)
        {
            _context.SimulationLabFeatures.Remove(f);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(ManageContent), new { id = sectionId });
    }

    // ================= INLINE EDIT ENDPOINTS (Admin only, all sections) =================
    // Same convention as News & Media's Quick* endpoints: JSON in, JSON out,
    // called via fetch() from a small pencil-icon + modal UI (no full-page
    // form posts / redirects), then the page reloads once to show the
    // fresh data. QuickUpdateSectionInfo / QuickSaveImage / QuickDeleteImage /
    // QuickSaveFeature / QuickDeleteFeature below all take a SectionId, so
    // they already work for the built-in 3 sections AND any admin-added
    // section -- only QuickCreateSection (right below) is new.

    public class QuickCreateSectionRequest
    {
        public string? SectionKey { get; set; }
        public string? Title { get; set; }
        public string? Tag { get; set; }
        public string? Description { get; set; }
        public string? IconClass { get; set; }
        public string? ColorHex { get; set; }
        public string? ColorHex2 { get; set; }
    }

    // POST: /SimulationLabs/QuickCreateSection
    // Lets an admin add a brand-new section (4th, 5th, ...) directly from
    // the "+ Add New Section" button on the public page, in edit mode --
    // no separate admin screen needed. The new row is saved straight to
    // the SimulationLabSections table, exactly like the original 3.
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateSection([FromBody] QuickCreateSectionRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Title is required." });
        }

        var baseKey = Slugify(string.IsNullOrWhiteSpace(req.SectionKey) ? req.Title : req.SectionKey);
        if (string.IsNullOrWhiteSpace(baseKey))
        {
            return BadRequest(new { success = false, message = "Could not derive a section key from the title. Please provide one manually." });
        }

        // Guarantee uniqueness even if another section already uses this key.
        var key = baseKey;
        var suffix = 2;
        while (await _context.SimulationLabSections.AnyAsync(s => s.SectionKey == key))
        {
            key = $"{baseKey}-{suffix}";
            suffix++;
        }

        var nextOrder = await _context.SimulationLabSections.CountAsync();

        var section = new SimulationLabSection
        {
            SectionKey = key,
            Title = req.Title.Trim(),
            Tag = string.IsNullOrWhiteSpace(req.Tag) ? null : req.Tag.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            IconClass = string.IsNullOrWhiteSpace(req.IconClass) ? "fas fa-flask" : req.IconClass.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(req.ColorHex) ? null : req.ColorHex.Trim(),
            ColorHex2 = string.IsNullOrWhiteSpace(req.ColorHex2) ? null : req.ColorHex2.Trim(),
            IsAvailable = true,
            DisplayOrder = nextOrder
        };

        _context.SimulationLabSections.Add(section);
        await _context.SaveChangesAsync();

        return Json(new { success = true, sectionId = section.SimulationLabSectionId, sectionKey = section.SectionKey });
    }

    // Turns a title like "Cabin Crew Trainer!" into "cabin-crew-trainer" for
    // use as the section's anchor id (#cabin-crew-trainer on the page).
    private static string Slugify(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        var lastWasDash = false;
        foreach (var ch in s)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && sb.Length > 0)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }
        return sb.ToString().TrimEnd('-');
    }

    public class QuickUpdateSectionInfoRequest
    {
        public int SectionId { get; set; }
        public string? Tag { get; set; }
        public string? Title { get; set; }
        public string? IconClass { get; set; }
        public string? Description { get; set; }
    }

    // POST: /SimulationLabs/QuickUpdateSectionInfo
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateSectionInfo([FromBody] QuickUpdateSectionInfoRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Title is required." });
        }

        var section = await _context.SimulationLabSections.FindAsync(req.SectionId);
        if (section == null)
        {
            return NotFound(new { success = false, message = "Section not found." });
        }

        section.Tag = string.IsNullOrWhiteSpace(req.Tag) ? null : req.Tag.Trim();
        section.Title = req.Title.Trim();
        section.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        section.IconClass = string.IsNullOrWhiteSpace(req.IconClass) ? section.IconClass : req.IconClass.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, title = section.Title });
    }

    public class QuickSaveImageRequest
    {
        public int? ImageId { get; set; }
        public int SectionId { get; set; }
        public bool IsLarge { get; set; }
        public string? Title { get; set; }
        public string? Badge { get; set; }
        public string? IconClass { get; set; }
        public string? ImageUrl { get; set; }
    }

    // POST: /SimulationLabs/QuickSaveImage  (adds a new small tile, or updates
    // an existing small tile / the single large tile -- called by the "Save"
    // button in the image modal.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickSaveImage([FromBody] QuickSaveImageRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Title))
        {
            return BadRequest(new { success = false, message = "Image title is required." });
        }

        SimulationLabImage? img;

        if (req.IsLarge)
        {
            // Only one large tile per section -- find (or create) it
            // regardless of which id the client happened to send.
            img = await _context.SimulationLabImages
                .FirstOrDefaultAsync(i => i.SimulationLabSectionId == req.SectionId && i.IsLarge);
            if (img == null)
            {
                img = new SimulationLabImage { SimulationLabSectionId = req.SectionId, IsLarge = true, DisplayOrder = 0 };
                _context.SimulationLabImages.Add(img);
            }
        }
        else if (req.ImageId.HasValue && req.ImageId.Value > 0)
        {
            img = await _context.SimulationLabImages
                .FirstOrDefaultAsync(i => i.SimulationLabImageId == req.ImageId.Value && !i.IsLarge);
            if (img == null)
            {
                return NotFound(new { success = false, message = "Image not found." });
            }
        }
        else
        {
            var nextOrder = await _context.SimulationLabImages.CountAsync(i => i.SimulationLabSectionId == req.SectionId && !i.IsLarge) + 1;
            img = new SimulationLabImage { SimulationLabSectionId = req.SectionId, IsLarge = false, DisplayOrder = nextOrder };
            _context.SimulationLabImages.Add(img);
        }

        img.Title = req.Title.Trim();
        img.Badge = string.IsNullOrWhiteSpace(req.Badge) ? null : req.Badge.Trim();
        img.IconClass = string.IsNullOrWhiteSpace(req.IconClass) ? null : req.IconClass.Trim();
        img.ImageUrl = string.IsNullOrWhiteSpace(req.ImageUrl) ? null : req.ImageUrl.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, imageId = img.SimulationLabImageId });
    }

    public class QuickDeleteImageRequest
    {
        public int ImageId { get; set; }
    }

    // POST: /SimulationLabs/QuickDeleteImage  (deletes any tile, large or
    // small. The large tile can be deleted too -- the page falls back to
    // its default placeholder icon/title when no large row exists, so the
    // section never ends up without a hero image slot.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteImage([FromBody] QuickDeleteImageRequest req)
    {
        if (req == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var img = await _context.SimulationLabImages.FindAsync(req.ImageId);
        if (img == null)
        {
            return NotFound(new { success = false, message = "Image not found (it may already be deleted)." });
        }

        _context.SimulationLabImages.Remove(img);
        await _context.SaveChangesAsync();

        return Json(new { success = true, imageId = req.ImageId });
    }

    public class QuickSaveFeatureRequest
    {
        public int? FeatureId { get; set; }
        public int SectionId { get; set; }
        public string? Label { get; set; }
        public string? IconClass { get; set; }
    }

    // POST: /SimulationLabs/QuickSaveFeature  (adds a new feature chip, or
    // updates an existing one -- called by the "Save" button in the feature
    // modal.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickSaveFeature([FromBody] QuickSaveFeatureRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Label))
        {
            return BadRequest(new { success = false, message = "Feature label is required." });
        }

        SimulationLabFeature? f;

        if (req.FeatureId.HasValue && req.FeatureId.Value > 0)
        {
            f = await _context.SimulationLabFeatures.FindAsync(req.FeatureId.Value);
            if (f == null)
            {
                return NotFound(new { success = false, message = "Feature not found." });
            }
        }
        else
        {
            var nextOrder = await _context.SimulationLabFeatures.CountAsync(x => x.SimulationLabSectionId == req.SectionId) + 1;
            f = new SimulationLabFeature { SimulationLabSectionId = req.SectionId, DisplayOrder = nextOrder };
            _context.SimulationLabFeatures.Add(f);
        }

        f.Label = req.Label.Trim();
        f.IconClass = string.IsNullOrWhiteSpace(req.IconClass) ? "fas fa-check-circle" : req.IconClass.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, featureId = f.SimulationLabFeatureId });
    }

    public class QuickDeleteFeatureRequest
    {
        public int FeatureId { get; set; }
    }

    // POST: /SimulationLabs/QuickDeleteFeature
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteFeature([FromBody] QuickDeleteFeatureRequest req)
    {
        if (req == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var f = await _context.SimulationLabFeatures.FindAsync(req.FeatureId);
        if (f == null)
        {
            return NotFound(new { success = false, message = "Feature not found (it may already be deleted)." });
        }

        _context.SimulationLabFeatures.Remove(f);
        await _context.SaveChangesAsync();

        return Json(new { success = true, featureId = req.FeatureId });
    }

    // ================= INFO CARD (sidebar heading + bullet list) =================
    // Same Quick* JSON convention as everything above. One InfoCard per
    // section (heading + optional highlight box); each InfoCard has many
    // InfoItems (the bullet list). If a section has no InfoCard, the view
    // renders nothing for that slot in public mode (no empty box) and an
    // "+ Add Info Card" placeholder in edit mode.

    public class QuickSaveInfoCardRequest
    {
        public int SectionId { get; set; }
        public string? Heading { get; set; }
        public string? HighlightText { get; set; }
        public string? HighlightIcon { get; set; }
        public string? HighlightColor { get; set; }
    }

    // POST: /SimulationLabs/QuickSaveInfoCard  (creates the section's info
    // card if it doesn't have one yet, otherwise updates heading/highlight.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickSaveInfoCard([FromBody] QuickSaveInfoCardRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Heading))
        {
            return BadRequest(new { success = false, message = "Heading is required." });
        }

        var card = await _context.SimulationLabInfoCards
            .FirstOrDefaultAsync(c => c.SimulationLabSectionId == req.SectionId);

        if (card == null)
        {
            card = new SimulationLabInfoCard { SimulationLabSectionId = req.SectionId };
            _context.SimulationLabInfoCards.Add(card);
        }

        card.Heading = req.Heading.Trim();
        card.HighlightText = string.IsNullOrWhiteSpace(req.HighlightText) ? null : req.HighlightText.Trim();
        card.HighlightIcon = string.IsNullOrWhiteSpace(req.HighlightIcon) ? null : req.HighlightIcon.Trim();
        card.HighlightColor = string.IsNullOrWhiteSpace(req.HighlightColor) ? null : req.HighlightColor.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, infoCardId = card.SimulationLabInfoCardId });
    }

    public class QuickDeleteInfoCardRequest
    {
        public int InfoCardId { get; set; }
    }

    // POST: /SimulationLabs/QuickDeleteInfoCard  (removes the whole card,
    // heading + all its bullets + highlight box -- the sidebar slot then
    // renders as nothing on the public page, and as "+ Add Info Card" in
    // edit mode.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteInfoCard([FromBody] QuickDeleteInfoCardRequest req)
    {
        if (req == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var card = await _context.SimulationLabInfoCards.FindAsync(req.InfoCardId);
        if (card == null)
        {
            return NotFound(new { success = false, message = "Info card not found (it may already be deleted)." });
        }

        _context.SimulationLabInfoCards.Remove(card);
        await _context.SaveChangesAsync();

        return Json(new { success = true, infoCardId = req.InfoCardId });
    }

    public class QuickSaveInfoItemRequest
    {
        public int? ItemId { get; set; }
        public int InfoCardId { get; set; }
        public string? Text { get; set; }
        public string? ColorHex { get; set; }
    }

    // POST: /SimulationLabs/QuickSaveInfoItem  (adds a new bullet, or
    // updates an existing one -- called by the "Save" button in the
    // info-item modal.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickSaveInfoItem([FromBody] QuickSaveInfoItemRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Text))
        {
            return BadRequest(new { success = false, message = "Item text is required." });
        }

        SimulationLabInfoItem? item;

        if (req.ItemId.HasValue && req.ItemId.Value > 0)
        {
            item = await _context.SimulationLabInfoItems.FindAsync(req.ItemId.Value);
            if (item == null)
            {
                return NotFound(new { success = false, message = "Item not found." });
            }
        }
        else
        {
            var nextOrder = await _context.SimulationLabInfoItems.CountAsync(x => x.SimulationLabInfoCardId == req.InfoCardId) + 1;
            item = new SimulationLabInfoItem { SimulationLabInfoCardId = req.InfoCardId, DisplayOrder = nextOrder };
            _context.SimulationLabInfoItems.Add(item);
        }

        item.Text = req.Text.Trim();
        item.ColorHex = string.IsNullOrWhiteSpace(req.ColorHex) ? null : req.ColorHex.Trim();

        await _context.SaveChangesAsync();

        return Json(new { success = true, itemId = item.SimulationLabInfoItemId });
    }

    public class QuickDeleteInfoItemRequest
    {
        public int ItemId { get; set; }
    }

    // POST: /SimulationLabs/QuickDeleteInfoItem
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteInfoItem([FromBody] QuickDeleteInfoItemRequest req)
    {
        if (req == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var item = await _context.SimulationLabInfoItems.FindAsync(req.ItemId);
        if (item == null)
        {
            return NotFound(new { success = false, message = "Item not found (it may already be deleted)." });
        }

        _context.SimulationLabInfoItems.Remove(item);
        await _context.SaveChangesAsync();

        return Json(new { success = true, itemId = req.ItemId });
    }

    public class QuickDeleteSectionRequest
    {
        public int SectionId { get; set; }
    }

    // POST: /SimulationLabs/QuickDeleteSection  (removes an admin-added
    // extra section -- 4th, 5th, ... -- straight from the Public page, the
    // same one-click way features/images are deleted. The 3 built-in
    // sections (Flight Simulators, ATC Simulator, Avionics Lab) can't be
    // removed this way since they're not rendered with a delete button,
    // but the check is repeated here server-side too.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteSection([FromBody] QuickDeleteSectionRequest req)
    {
        if (req == null)
        {
            return BadRequest(new { success = false, message = "Invalid request." });
        }

        var section = await _context.SimulationLabSections.FindAsync(req.SectionId);
        if (section == null)
        {
            return NotFound(new { success = false, message = "Section not found (it may already be deleted)." });
        }

        var knownKeys = new[] { FlightSectionKey, AtcSectionKey, AvionicsSectionKey };
        if (knownKeys.Contains(section.SectionKey))
        {
            return BadRequest(new { success = false, message = "The built-in sections can't be deleted." });
        }

        _context.SimulationLabSections.Remove(section);
        await _context.SaveChangesAsync();

        return Json(new { success = true, sectionId = req.SectionId });
    }

    // ================= IMAGE UPLOAD (Admin only, Section 1) =================
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB, same limit as /Cms/UploadImage

    // POST: /SimulationLabs/UploadSectionImage  (accepts a real image file
    // from the image modal's file picker, stores it under wwwroot/uploads,
    // records it in MediaLibrary -- same convention as
    // /NewsPosts/UploadGalleryPhoto -- and returns the public URL to store
    // on the large/small image row.)
    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxImageSizeBytes)]
    public async Task<IActionResult> UploadSectionImage(IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No file received." });
        }

        if (file.Length > MaxImageSizeBytes)
        {
            return BadRequest(new { success = false, message = "File is too large. Max size is 5 MB." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(ext))
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
}

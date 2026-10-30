using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;

namespace NAU_Web.Controllers
{
    // Serves the public Facilities overview page (/facilities.html) and all
    // individual facility pages (/facility-library.html, /facility-cafeteria.html,
    // /facility-hostel.html, /facility-mess.html, /facility-sports.html,
    // /facility-medical.html, /facility-masjid.html, /facility-smart-campus.html).
    // All content is stored in the database (Pages table) and is editable inline by
    // an Admin when visiting with ?edit=true, following the project's standard pattern.
    public class FacilitiesController : Controller
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;

        public const string FacilitiesOverviewSlug = "facilities";
        public const string LibrarySlug = "facility-library";
        public const string CafeteriaSlug = "facility-cafeteria";
        public const string HostelSlug = "facility-hostel";
        public const string MessSlug = "facility-mess";
        public const string SportsSlug = "facility-sports";
        public const string MedicalSlug = "facility-medical";
        public const string MasjidSlug = "facility-masjid";
        public const string SmartCampusSlug = "facility-smart-campus";

        public FacilitiesController(
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

        private async Task<bool> CanUserEditFacilityAsync(string slug)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated) return false;
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;
            return await _permissionService.HasFacilityPermissionAsync(currentUser.Id, slug);
        }

        private async Task<bool> CheckEditModeAsync(string? edit, string slug)
        {
            if (string.IsNullOrEmpty(edit)) return false;
            if (!(edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase))) return false;
            return await CanUserEditFacilityAsync(slug);
        }

        // ================= MAIN FACILITIES OVERVIEW PAGE =================

        // GET: /facilities.html
        // GET: /Facilities/Public
        // GET: /Facilities
        [AllowAnonymous]
        [HttpGet("/facilities.html")]
        [HttpGet("/Facilities/Public")]
        [HttpGet("/Facilities")]
        [HttpGet("/Facilities/Index")]
        public async Task<IActionResult> Public(string? edit = null)
        {
            await SetCurrentUserNameAsync();
            var canEdit = await CanUserEditFacilityAsync(FacilitiesOverviewSlug);
            ViewBag.EditMode = await CheckEditModeAsync(edit, FacilitiesOverviewSlug);
            ViewBag.IsAdminViewer = canEdit;

            var page = await LoadOrSeedPageAsync(
                FacilitiesOverviewSlug,
                "Facilities - National Aviation University",
                "facilities-content.html");

            return View("Public", page);
        }

        // ================= INDIVIDUAL FACILITY DETAIL PAGES =================

        // GET: /facility-library.html
        [AllowAnonymous]
        [HttpGet("/facility-library.html")]
        public async Task<IActionResult> Library(string? edit = null)
        {
            return await RenderFacilityDetail(LibrarySlug, "Library & Resource Center - National Aviation University", "facility-library-content.html", edit);
        }

        // GET: /facility-cafeteria.html
        [AllowAnonymous]
        [HttpGet("/facility-cafeteria.html")]
        public async Task<IActionResult> Cafeteria(string? edit = null)
        {
            return await RenderFacilityDetail(CafeteriaSlug, "Cafeteria & Dining - National Aviation University", "facility-cafeteria-content.html", edit);
        }

        // GET: /facility-hostel.html
        // GET: /facility-hostel__1_.html
        [AllowAnonymous]
        [HttpGet("/facility-hostel.html")]
        [HttpGet("/facility-hostel__1_.html")]
        public async Task<IActionResult> Hostel(string? edit = null)
        {
            return await RenderFacilityDetail(HostelSlug, "Hostel - National Aviation University", "facility-hostel-content.html", edit);
        }

        // GET: /facility-mess.html
        [AllowAnonymous]
        [HttpGet("/facility-mess.html")]
        public async Task<IActionResult> Mess(string? edit = null)
        {
            return await RenderFacilityDetail(MessSlug, "Mess - National Aviation University", "facility-mess-content.html", edit);
        }

        // GET: /facility-sports.html
        [AllowAnonymous]
        [HttpGet("/facility-sports.html")]
        public async Task<IActionResult> Sports(string? edit = null)
        {
            return await RenderFacilityDetail(SportsSlug, "Sports Complex - National Aviation University", "facility-sports-content.html", edit);
        }

        // GET: /facility-medical.html
        [AllowAnonymous]
        [HttpGet("/facility-medical.html")]
        public async Task<IActionResult> Medical(string? edit = null)
        {
            return await RenderFacilityDetail(MedicalSlug, "Medical Center - National Aviation University", "facility-medical-content.html", edit);
        }

        // GET: /facility-masjid.html
        // GET: /facility-mosque.html
        [AllowAnonymous]
        [HttpGet("/facility-masjid.html")]
        [HttpGet("/facility-mosque.html")]
        public async Task<IActionResult> Masjid(string? edit = null)
        {
            return await RenderFacilityDetail(MasjidSlug, "Mosque - National Aviation University", "facility-masjid-content.html", edit);
        }

        // GET: /facility-smart-campus.html
        [AllowAnonymous]
        [HttpGet("/facility-smart-campus.html")]
        public async Task<IActionResult> SmartCampus(string? edit = null)
        {
            return await RenderFacilityDetail(SmartCampusSlug, "Smart Campus - National Aviation University", "facility-smart-campus-content.html", edit);
        }

        // GET: /Facilities/PublicDetails?slug=...
        // GET: /Facilities/Detail/{slug}
        [AllowAnonymous]
        [HttpGet("/Facilities/PublicDetails")]
        [HttpGet("/Facilities/Detail/{slug}")]
        [HttpGet("/Facilities/Details/{slug}")]
        public async Task<IActionResult> PublicDetails(string? slug = null, string? edit = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Public));
            }

            var normalizedSlug = NormalizeFacilitySlug(slug);
            var (title, seedFile) = GetFacilityMetadata(normalizedSlug);

            return await RenderFacilityDetail(normalizedSlug, title, seedFile, edit);
        }

        private async Task<IActionResult> RenderFacilityDetail(string slug, string title, string seedFileName, string? edit)
        {
            await SetCurrentUserNameAsync();
            var canEdit = await CanUserEditFacilityAsync(slug);
            ViewBag.EditMode = await CheckEditModeAsync(edit, slug);
            ViewBag.IsAdminViewer = canEdit;
            ViewBag.PageSlug = slug;

            var page = await LoadOrSeedPageAsync(slug, title, seedFileName);
            return View("Detail", page);
        }

        private string NormalizeFacilitySlug(string slug)
        {
            slug = slug.Trim().ToLowerInvariant();
            if (slug.StartsWith("facility-")) return slug;

            return slug switch
            {
                "library" => LibrarySlug,
                "cafeteria" => CafeteriaSlug,
                "hostel" => HostelSlug,
                "mess" => MessSlug,
                "sports" or "sports-complex" => SportsSlug,
                "medical" or "medical-center" => MedicalSlug,
                "masjid" or "mosque" => MasjidSlug,
                "smart" or "smart-campus" => SmartCampusSlug,
                _ => $"facility-{slug}"
            };
        }

        private (string Title, string SeedFile) GetFacilityMetadata(string slug)
        {
            return slug switch
            {
                LibrarySlug => ("Library & Resource Center - National Aviation University", "facility-library-content.html"),
                CafeteriaSlug => ("Cafeteria & Dining - National Aviation University", "facility-cafeteria-content.html"),
                HostelSlug => ("Hostel - National Aviation University", "facility-hostel-content.html"),
                MessSlug => ("Mess - National Aviation University", "facility-mess-content.html"),
                SportsSlug => ("Sports Complex - National Aviation University", "facility-sports-content.html"),
                MedicalSlug => ("Medical Center - National Aviation University", "facility-medical-content.html"),
                MasjidSlug => ("Mosque - National Aviation University", "facility-masjid-content.html"),
                SmartCampusSlug => ("Smart Campus - National Aviation University", "facility-smart-campus-content.html"),
                _ => ("Facility - National Aviation University", "facilities-content.html")
            };
        }

        public static string GetLiveAnnouncementBarHtml(string? contentRootPath = null)
        {
            return AnnouncementService.GenerateAnnouncementBarHtml(new AnnouncementSetting());
        }

        public static string GetLiveNavBarHtml(string? contentRootPath = null)
        {
            return NavMenuService.GenerateNavBarHtml(_ => false);
        }

        public static string NormalizeFacilityContentHtml(string? html, string? contentRootPath = null)
        {
            if (string.IsNullOrWhiteSpace(html)) return html ?? "";

            var annBar = GetLiveAnnouncementBarHtml(contentRootPath);
            var header = annBar + "\n\n" + GetLiveNavBarHtml(contentRootPath);

            // 1. Replace the entire header block (from announcement to </nav>)
            var announcementIdx = html.IndexOf("<div class=\"announcement\"", StringComparison.OrdinalIgnoreCase);
            if (announcementIdx >= 0)
            {
                var navEndIdx = html.IndexOf("</nav>", announcementIdx, StringComparison.OrdinalIgnoreCase);
                if (navEndIdx >= 0)
                {
                    navEndIdx += "</nav>".Length;
                    html = html.Substring(0, announcementIdx) + header + html.Substring(navEndIdx);
                }
            }

            // 2. Remove conflicting inline header/announcement CSS overrides
            html = System.Text.RegularExpressions.Regex.Replace(
                html,
                @"\.announcement\s*\.scroll\s*span\s*\{[^}]*\}",
                ".announcement .scroll span, .scroll span { font-weight: 700 !important; background: transparent !important; color: #f5a623 !important; padding: 0 !important; border-radius: 0 !important; font-size: inherit !important; letter-spacing: normal !important; margin-right: 6px !important; display: inline !important; }",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 3. Remove stray menu-item active highlights in content
            html = html.Replace("<div class=\"menu-item active\">", "<div class=\"menu-item\">");
            html = html.Replace("<a href=\"contact.html\" class=\"nav-link\">Contact Us</a>", "<a href=\"/Contact\" class=\"nav-link\">Contact</a>");

            return html;
        }

        public static readonly string CanonicalNavHtml = @"<!-- NAVBAR -->
<nav class=""navbar"">
  <a href=""/"" class=""logo"">
    <img src=""/images/logo.png"" alt=""NAU Logo"" onerror=""this.style.display='none'"" style=""height:52px;width:auto;display:block;object-fit:contain;"">
    <div style=""display:flex;flex-direction:column;justify-content:center;gap:1px;"">
      <span style=""font-size:24px;font-weight:900;color:#0b2c4d;font-family:'Montserrat',sans-serif;line-height:1.1;letter-spacing:0px;"">National Aviation University</span>
      <span style=""font-size:12px;font-weight:500;color:#474b50;letter-spacing:2.5px;font-family:'Open Sans',sans-serif;line-height:1;"">LEARN &middot; LEAD &middot; FLY HIGH</span>
    </div>
  </a>

  <button type=""button"" class=""nav-toggle-btn"" aria-label=""Toggle navigation menu""><i class=""fas fa-bars""></i></button>

  <div class=""menu"">

    <!-- ACADEMICS -->
    <div class=""menu-item"">
      <a href=""/Faculties/Public"" class=""nav-link"">Academics <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"">
            <div class=""dd-col-title"">Faculties</div>
            <a href=""/Faculties/Detail/engineering"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-cogs""></i></span><span class=""dd-text""><strong>Faculty of Engineering</strong><span>ANS Engineering &amp; Airport Engineering</span></span></a>
            <a href=""/Faculties/Detail/information-technology-cyber-security"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-laptop-code""></i></span><span class=""dd-text""><strong>Faculty of IT &amp; Cyber Security </strong><span>Aviation Cyber Security</span></span></a>
            <a href=""/Faculties/Detail/operations"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-plane-departure""></i></span><span class=""dd-text""><strong>Faculty of Aviation Operations</strong><span>Air Traffic Management &amp; Airport Operations</span></span></a>
            <a href=""/Faculties/Detail/management"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-chart-line""></i></span><span class=""dd-text""><strong>Faculty of Management Sciences</strong><span>Aviation Management &amp; MBA</span></span></a>
            <a href=""/Faculties/Public"" class=""dd-view-all"">View All Faculties <i class=""fas fa-arrow-right""></i></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Schools</div>
            <div class=""dd-school-item"">
              <a href=""/Courses/Public?school=navigation"" class=""dd-link dd-school-link""><span class=""dd-icon""><i class=""fas fa-broadcast-tower""></i></span><span class=""dd-text""><strong>School of Air Navigation Services (ANS)</strong><span>Air Traffic Control &amp; CNS/ATM Systems</span></span><i class=""fas fa-chevron-right dd-school-arrow"" onclick=""event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);""></i></a>
              <div class=""dd-submenu"" data-school=""navigation""></div>
            </div>
            <div class=""dd-school-item"">
              <a href=""/Courses/Public?school=regulatory"" class=""dd-link dd-school-link""><span class=""dd-icon""><i class=""fas fa-scale-balanced""></i></span><span class=""dd-text""><strong>School of Regulatory Services</strong><span>Aviation Safety &amp; Regulatory Compliance</span></span><i class=""fas fa-chevron-right dd-school-arrow"" onclick=""event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);""></i></a>
              <div class=""dd-submenu"" data-school=""regulatory""></div>
            </div>
            <div class=""dd-school-item"">
              <a href=""/Courses/Public?school=airport"" class=""dd-link dd-school-link""><span class=""dd-icon""><i class=""fas fa-fire-extinguisher""></i></span><span class=""dd-text""><strong>School of Airport Services</strong><span>Ground Operations &amp; Rescue/Firefighting</span></span><i class=""fas fa-chevron-right dd-school-arrow"" onclick=""event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);""></i></a>
              <div class=""dd-submenu"" data-school=""airport""></div>
            </div>
            <div class=""dd-school-item"">
              <a href=""/Courses/Public?school=management"" class=""dd-link dd-school-link""><span class=""dd-icon""><i class=""fas fa-briefcase""></i></span><span class=""dd-text""><strong>School of Aviation Management</strong><span>Leadership &amp; Management Training</span></span><i class=""fas fa-chevron-right dd-school-arrow"" onclick=""event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);""></i></a>
              <div class=""dd-submenu"" data-school=""management""></div>
            </div>
            <a href=""/Courses/Public"" class=""dd-view-all"">View All Schools <i class=""fas fa-arrow-right""></i></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Student Resources</div>
            <a href=""/Account/Login"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-user-circle""></i></span><span class=""dd-text""><strong>Online Portal</strong><span>Access results, schedules &amp; fee challan</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openResults(event)""><span class=""dd-icon""><i class=""fas fa-poll""></i></span><span class=""dd-text""><strong>Results</strong><span>Check your exam results</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openCalendar(event)""><span class=""dd-icon""><i class=""fas fa-calendar-alt""></i></span><span class=""dd-text""><strong>Academic Calendar</strong><span>Semester dates and schedules</span></span></a>
            <a href=""/CourseCatalog"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-book-open""></i></span><span class=""dd-text""><strong>Course Catalog</strong><span>All programs and syllabi</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openExamSchedule(event)""><span class=""dd-icon""><i class=""fas fa-file-alt""></i></span><span class=""dd-text""><strong>Examination Schedule</strong><span>Mid &amp; final term timetables</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openDownloads(event)""><span class=""dd-icon""><i class=""fas fa-download""></i></span><span class=""dd-text""><strong>Downloads</strong><span>Forms, guides &amp; documents</span></span></a>
          </div>
        </div>
      </div>
    </div>

    <!-- ADMISSIONS -->
    <div class=""menu-item"">
      <a href=""/Account/Register"" class=""nav-link"">Admissions <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"">
            <div class=""dd-col-title"">How to Apply</div>
            <a href=""/Account/Register"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-paper-plane""></i></span><span class=""dd-text""><strong>Apply Online</strong><span>Submit your application now</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openEligibility(event)""><span class=""dd-icon""><i class=""fas fa-list-check""></i></span><span class=""dd-text""><strong>Eligibility Criteria</strong><span>Entry requirements per program</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openRequiredDocs(event)""><span class=""dd-icon""><i class=""fas fa-file-upload""></i></span><span class=""dd-text""><strong>Required Documents</strong><span>Documents needed for admission</span></span></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Financial</div>
            <a href=""/financial.html#fee-structure"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-coins""></i></span><span class=""dd-text""><strong>Fee Structure</strong><span>Semester-wise fee breakdown</span></span></a>
            <a href=""/financial.html#scholarships"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-hand-holding-usd""></i></span><span class=""dd-text""><strong>Scholarships</strong><span>Merit and need-based aid</span></span></a>
            <a href=""/financial.html#payment-methods"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-receipt""></i></span><span class=""dd-text""><strong>Payment Methods</strong><span>Bank and online payment options</span></span></a>
          </div>
          <div class=""dd-col"" style=""flex:0 0 220px;"">
            <a href=""/Account/Register"" class=""dd-featured"">
              <div class=""feat-label"">Admissions Open 2026</div>
              <div class=""feat-title"">Begin Your Aviation Career Today</div>
              <div class=""feat-desc"">Applications are being accepted for Fall 2026. Limited seats available.</div>
              <span class=""feat-btn"">Apply Now</span>
            </a>
          </div>
        </div>
      </div>
    </div>

    <!-- TRAINING -->
    <div class=""menu-item"">
      <a href=""/SimulationLabs/Public"" class=""nav-link"">Training <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"">
            <div class=""dd-col-title"">Simulation &amp; Labs</div>
            <a href=""/SimulationLabs/Public#flight-simulators"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-plane""></i></span><span class=""dd-text""><strong>Flight Simulators</strong><span>State-of-the-art FFS &amp; FTD devices</span></span></a>
            <a href=""/SimulationLabs/Public#atc-simulator"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-tower-broadcast""></i></span><span class=""dd-text""><strong>ATC Simulator</strong><span>Live radar and voice simulation</span></span></a>
            <a href=""/SimulationLabs/Public#avionics-lab"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-microchip""></i></span><span class=""dd-text""><strong>Avionics Lab</strong><span>Practical avionics systems training</span></span></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Professional Courses</div>
            <a href=""#"" class=""dd-link"" onclick=""openProfCourses(event,'pcaa')""><span class=""dd-icon""><i class=""fas fa-user-graduate""></i></span><span class=""dd-text""><strong>PCAA Type Ratings</strong><span>Endorsed by Civil Aviation Authority</span></span></a>
            <a href=""#"" class=""dd-link"" onclick=""openProfCourses(event,'icao')""><span class=""dd-icon""><i class=""fas fa-globe""></i></span><span class=""dd-text""><strong>ICAO Courses</strong><span>Internationally recognized programs</span></span></a>
          </div>
        </div>
      </div>
    </div>

    <!-- CAMPUS LIFE -->
    <div class=""menu-item"">
      <a href=""/facilities.html"" class=""nav-link"">Campus Life <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"">
            <div class=""dd-col-title"">Facilities</div>
            <a href=""/facility-library.html"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-book""></i></span><span class=""dd-text""><strong>Library &amp; Resource Center</strong><span>Digital and physical collections</span></span></a>
            <a href=""/facility-cafeteria.html"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-utensils""></i></span><span class=""dd-text""><strong>Cafeteria &amp; Dining</strong><span>On-campus food services</span></span></a>
            <a href=""/facility-hostel.html"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-bed""></i></span><span class=""dd-text""><strong>Hostel</strong><span>On-campus residential accommodation</span></span></a>
            <a href=""/facility-mess.html"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-utensils""></i></span><span class=""dd-text""><strong>Mess</strong><span>Student dining and meal services</span></span></a>
            <a href=""/facilities.html"" class=""dd-view-all"">View All Facilities <i class=""fas fa-arrow-right""></i></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Student Life</div>
            <a href=""#"" class=""dd-link"" onclick=""openEvents(event)""><span class=""dd-icon""><i class=""fas fa-calendar-check""></i></span><span class=""dd-text""><strong>Events &amp; Activities</strong><span>Seminars, trips and cultural days</span></span></a>
            <a href=""https://paa.gov.pk/media/job-opportunities"" target=""_blank"" rel=""noopener noreferrer"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-briefcase""></i></span><span class=""dd-text""><strong>Career Services</strong><span>Placement and internship support</span></span></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">News &amp; Media</div>
            <a href=""/NewsPosts/Public#latest-news"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-newspaper""></i></span><span class=""dd-text""><strong>Latest News</strong><span>University announcements</span></span></a>
            <a href=""/NewsPosts/Public#photo-gallery"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-images""></i></span><span class=""dd-text""><strong>Photo Gallery</strong><span>Campus life in pictures</span></span></a>
            <a href=""/NewsPosts/Public#video-gallery"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-video""></i></span><span class=""dd-text""><strong>Video Gallery</strong><span>Events and training videos</span></span></a>
          </div>
        </div>
      </div>
    </div>

    <!-- USEFUL LINKS -->
    <div class=""menu-item"">
      <a href=""/links.html"" class=""nav-link"">Useful Links <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"" style=""flex:0 0 auto;min-width:260px;"">
            <div class=""dd-col-title"">Useful links</div>
            <a href=""https://cati-lms.pitb.gov.pk/"" target=""_blank"" rel=""noopener noreferrer"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-flag""></i></span><span class=""dd-text""><strong>LMS</strong></span></a>
            <a href=""https://cati-elibrary.pitb.gov.pk/"" target=""_blank"" rel=""noopener noreferrer"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-landmark""></i></span><span class=""dd-text""><strong>E-library</strong></span></a>
            <a href=""/links.html"" class=""dd-view-all"">View All Links <i class=""fas fa-arrow-right""></i></a>
          </div>
        </div>
      </div>
    </div>

    <!-- ABOUT -->
    <div class=""menu-item"">
      <a href=""/about.html"" class=""nav-link"">About <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"" style=""flex:0 0 auto;min-width:260px;"">
            <div class=""dd-col-title"">About</div>
            <a href=""/about.html#history-caa"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-flag""></i></span><span class=""dd-text""><strong>History of CAA in Pakistan</strong><span>Evolution of Civil Aviation Authority</span></span></a>
            <a href=""/about.html#history-cati"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-landmark""></i></span><span class=""dd-text""><strong>History of CATI</strong><span>Our journey and founding milestones</span></span></a>
            <a href=""/about.html#management"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-sitemap""></i></span><span class=""dd-text""><strong>Management</strong><span>Leadership and organizational structure</span></span></a>
          </div>
          <div class=""dd-col"" style=""flex:0 0 220px;margin-left:auto;"">
            <a href=""/about.html"" class=""dd-featured"">
              <div class=""feat-label"">Established 2026</div>
              <div class=""feat-title"">Pakistan's Premier Aviation Institution</div>
              <div class=""feat-desc"">Located at Airport Road, Hyderabad — shaping future aviation leaders.</div>
              <span class=""feat-btn"">Learn More</span>
            </a>
          </div>
        </div>
      </div>
    </div>

    <!-- CONTACT -->
    <div class=""menu-item"">
      <a href=""/Contact"" class=""nav-link"">Contact <i class=""fas fa-chevron-down chevron""></i></a>
      <div class=""dropdown"">
        <div class=""dd-inner"">
          <div class=""dd-col"">
            <div class=""dd-col-title"">Get In Touch</div>
            <a href=""/Contact"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-map-marker-alt""></i></span><span class=""dd-text""><strong>Visit Us</strong><span>Airport Road, Hyderabad, Sindh</span></span></a>
            <a href=""tel:+92229260310"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-phone""></i></span><span class=""dd-text""><strong>Call Us</strong><span>+92 22 9260310-19</span></span></a>
            <a href=""mailto:AdID.Academics@paa.gov.pk"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-envelope""></i></span><span class=""dd-text""><strong>Email Us</strong><span>AdID.Academics@paa.gov.pk</span></span></a>
          </div>
          <div class=""dd-col"">
            <div class=""dd-col-title"">Departments</div>
            <a href=""mailto:admissions@au.edu.pk"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-user-graduate""></i></span><span class=""dd-text""><strong>Admissions Office</strong><span>admissions@au.edu.pk</span></span></a>
            <a href=""mailto:research@au.edu.pk"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-microscope""></i></span><span class=""dd-text""><strong>Research Office</strong><span>research@au.edu.pk</span></span></a>
            <a href=""mailto:pr@au.edu.pk"" class=""dd-link""><span class=""dd-icon""><i class=""fas fa-comments""></i></span><span class=""dd-text""><strong>Public Relations</strong><span>pr@au.edu.pk</span></span></a>
          </div>
          <div class=""dd-col"" style=""flex:0 0 220px;"">
            <a href=""/Account/Login"" class=""dd-featured"">
              <div class=""feat-label"">Student Portal</div>
              <div class=""feat-title"">Access Your Online Portal</div>
              <div class=""feat-desc"">Check results, schedules, fee challan and more from anywhere.</div>
              <span class=""feat-btn"">Sign In</span>
            </a>
          </div>
        </div>
      </div>
    </div>

    <!-- SEARCH -->
    <div class=""search-wrap"">
      <i class=""fas fa-search search-icon"" onclick=""toggleSearch(event)""></i>
      <input id=""searchBox"" class=""search-box"" placeholder=""Search..."" onblur=""closeSearch()"">
    </div>

  </div>
</nav>";

        private async Task<Page> LoadOrSeedPageAsync(string slug, string title, string seedFileName)
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == slug);

            var seedPath = Path.Combine(_env.ContentRootPath, "SeedData", seedFileName);
            var seedHtml = System.IO.File.Exists(seedPath)
                ? await System.IO.File.ReadAllTextAsync(seedPath)
                : "<p>Content not found. Please contact the administrator.</p>";

            seedHtml = NormalizeFacilityContentHtml(seedHtml, _env.ContentRootPath);

            bool needsSeeding = page == null
                || string.IsNullOrWhiteSpace(page.ContentHtml)
                || page.ContentHtml.Contains("Content not found. Please contact the administrator.");

            if (needsSeeding)
            {
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
                        page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == slug) ?? page;
                    }
                }
                else if (!seedHtml.Contains("Content not found. Please contact the administrator."))
                {
                    page.ContentHtml = seedHtml;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            else if (page != null)
            {
                // Ensure existing database content has normalized canonical header
                var normalized = NormalizeFacilityContentHtml(page.ContentHtml, _env.ContentRootPath);
                if (normalized != page.ContentHtml)
                {
                    page.ContentHtml = normalized;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            page ??= new Page
            {
                Slug = slug,
                Title = title,
                ContentHtml = seedHtml,
                IsPublished = true,
                UpdatedAt = DateTime.UtcNow
            };

            return page;
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class NavMenuService : INavMenuService
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<string, bool> _cache = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isSeeded = false;
        private static readonly object _seedLock = new();

        public NavMenuService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task EnsureSeededAsync()
        {
            if (_isSeeded) return;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();

            // Ensure database table exists
            try
            {
                var count = await db.NavMenuSettings.CountAsync();
                if (count > 0)
                {
                    _isSeeded = true;
                    await RefreshCacheAsync(db);
                    return;
                }
            }
            catch
            {
                // Table might not exist yet; create schema if needed
                try
                {
                    await db.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NavMenuSettings')
                        BEGIN
                            CREATE TABLE [NavMenuSettings] (
                                [Id] INT IDENTITY(1,1) PRIMARY KEY,
                                [MenuKey] NVARCHAR(100) NOT NULL,
                                [ParentKey] NVARCHAR(100) NULL,
                                [Category] NVARCHAR(50) NOT NULL,
                                [Title] NVARCHAR(150) NOT NULL,
                                [Description] NVARCHAR(250) NULL,
                                [IsHidden] BIT NOT NULL DEFAULT 0,
                                [DisplayOrder] INT NOT NULL DEFAULT 0,
                                [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
                            );
                            CREATE UNIQUE INDEX [IX_NavMenuSettings_MenuKey] ON [NavMenuSettings] ([MenuKey]);
                        END
                    ");
                }
                catch { }
            }

            var defaultSettings = GetDefaultMenuHierarchy();
            foreach (var s in defaultSettings)
            {
                var existing = await db.NavMenuSettings.FirstOrDefaultAsync(x => x.MenuKey == s.MenuKey);
                if (existing == null)
                {
                    db.NavMenuSettings.Add(s);
                }
            }
            await db.SaveChangesAsync();
            _isSeeded = true;
            await RefreshCacheAsync(db);
        }

        private async Task RefreshCacheAsync(NauDbContext db)
        {
            var all = await db.NavMenuSettings.AsNoTracking().ToListAsync();
            _cache.Clear();
            foreach (var item in all)
            {
                _cache[item.MenuKey] = item.IsHidden;
            }
        }

        public async Task<List<NavMenuSetting>> GetAllSettingsAsync()
        {
            await EnsureSeededAsync();
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            return await db.NavMenuSettings.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync();
        }

        public async Task<bool> IsHiddenAsync(string menuKey)
        {
            if (string.IsNullOrEmpty(menuKey)) return false;
            await EnsureSeededAsync();
            if (_cache.TryGetValue(menuKey, out var hidden)) return hidden;

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            var item = await db.NavMenuSettings.FirstOrDefaultAsync(x => x.MenuKey == menuKey);
            var isHidden = item != null && item.IsHidden;
            _cache[menuKey] = isHidden;
            return isHidden;
        }

        public bool IsHidden(string menuKey)
        {
            if (string.IsNullOrEmpty(menuKey)) return false;
            if (_cache.TryGetValue(menuKey, out var hidden)) return hidden;
            
            // If cache miss, fire async task non-blocking or fallback to false
            _ = EnsureSeededAsync();
            return false;
        }

        public async Task<bool> ToggleVisibilityAsync(string menuKey, bool? isHidden = null)
        {
            if (string.IsNullOrEmpty(menuKey)) return false;
            await EnsureSeededAsync();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            var item = await db.NavMenuSettings.FirstOrDefaultAsync(x => x.MenuKey == menuKey);
            if (item == null)
            {
                var defaultHierarchy = GetDefaultMenuHierarchy();
                var def = defaultHierarchy.FirstOrDefault(x => x.MenuKey.Equals(menuKey, StringComparison.OrdinalIgnoreCase));
                if (def != null)
                {
                    item = def;
                    db.NavMenuSettings.Add(item);
                }
                else
                {
                    return false;
                }
            }

            item.IsHidden = isHidden ?? !item.IsHidden;
            item.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _cache[menuKey] = item.IsHidden;
            return item.IsHidden;
        }

        public async Task ResetDefaultsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NauDbContext>();
            var all = await db.NavMenuSettings.ToListAsync();
            foreach (var item in all)
            {
                item.IsHidden = false;
                item.UpdatedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
            _cache.Clear();
            foreach (var item in all)
            {
                _cache[item.MenuKey] = false;
            }
        }

        public async Task<string> GetRenderedNavBarHtmlAsync()
        {
            await EnsureSeededAsync();
            SiteBrandingSetting? branding = null;
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var brandingService = scope.ServiceProvider.GetService<ISiteBrandingService>();
                if (brandingService != null)
                {
                    branding = await brandingService.GetSettingAsync();
                }
            }
            catch { }

            return GenerateNavBarHtml(k => IsHidden(k), branding);
        }

        public static string GenerateNavBarHtml(Func<string, bool> isHidden, SiteBrandingSetting? branding = null)
        {
            var logo = !string.IsNullOrWhiteSpace(branding?.LogoUrl) ? branding.LogoUrl : "/images/logo.png";
            var title = !string.IsNullOrWhiteSpace(branding?.SiteTitle) ? branding.SiteTitle : "National Aviation University";
            var tagline = !string.IsNullOrWhiteSpace(branding?.Tagline) ? branding.Tagline : "LEARN · LEAD · FLY HIGH";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<nav class=\"navbar\">");
            sb.AppendLine("  <a href=\"/\" class=\"logo\">");
            sb.AppendLine($"    <img src=\"{System.Net.WebUtility.HtmlEncode(logo)}\" alt=\"{System.Net.WebUtility.HtmlEncode(title)} Logo\" onerror=\"this.style.display='none'\" style=\"height:52px;width:auto;display:block;object-fit:contain;\">");
            sb.AppendLine("    <div style=\"display:flex;flex-direction:column;justify-content:center;gap:1px;\">");
            sb.AppendLine($"      <span style=\"font-size:24px;font-weight:900;color:#0b2c4d;font-family:'Montserrat',sans-serif;line-height:1.1;letter-spacing:0px;\">{System.Net.WebUtility.HtmlEncode(title)}</span>");
            sb.AppendLine($"      <span style=\"font-size:12px;font-weight:500;color:#474b50;letter-spacing:2.5px;font-family:'Open Sans',sans-serif;line-height:1;\">{System.Net.WebUtility.HtmlEncode(tagline)}</span>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </a>");
            sb.AppendLine("");
            sb.AppendLine("  <button type=\"button\" class=\"nav-toggle-btn\" aria-label=\"Toggle navigation menu\"><i class=\"fas fa-bars\"></i></button>");
            sb.AppendLine("");
            sb.AppendLine("  <div class=\"menu\">");

            // 1. ACADEMICS
            if (!isHidden("menu_academics"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/Faculties/Public\" class=\"nav-link\">Academics <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_academics_faculties"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Faculties</div>");
                    if (!isHidden("item_fac_engineering")) sb.AppendLine("            <a href=\"/Faculties/Detail/engineering\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-cogs\"></i></span><span class=\"dd-text\"><strong>Faculty of Engineering</strong><span>ANS Engineering &amp; Airport Engineering</span></span></a>");
                    if (!isHidden("item_fac_it")) sb.AppendLine("            <a href=\"/Faculties/Detail/information-technology-cyber-security\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-laptop-code\"></i></span><span class=\"dd-text\"><strong>Faculty of IT &amp; Cyber Security </strong><span>Aviation Cyber Security</span></span></a>");
                    if (!isHidden("item_fac_operations")) sb.AppendLine("            <a href=\"/Faculties/Detail/operations\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-plane-departure\"></i></span><span class=\"dd-text\"><strong>Faculty of Aviation Operations</strong><span>Air Traffic Management &amp; Airport Operations</span></span></a>");
                    if (!isHidden("item_fac_management")) sb.AppendLine("            <a href=\"/Faculties/Detail/management\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-chart-line\"></i></span><span class=\"dd-text\"><strong>Faculty of Management Sciences</strong><span>Aviation Management &amp; MBA</span></span></a>");
                    if (!isHidden("item_fac_viewall")) sb.AppendLine("            <a href=\"/Faculties/Public\" class=\"dd-view-all\">View All Faculties <i class=\"fas fa-arrow-right\"></i></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_academics_schools"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Schools</div>");
                    if (!isHidden("item_school_ans")) {
                        sb.AppendLine("            <div class=\"dd-school-item\">");
                        sb.AppendLine("              <a href=\"/Courses/Public?school=navigation\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-broadcast-tower\"></i></span><span class=\"dd-text\"><strong>School of Air Navigation Services (ANS)</strong><span>Air Traffic Control &amp; CNS/ATM Systems</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>");
                        sb.AppendLine("              <div class=\"dd-submenu\" data-school=\"navigation\"></div>");
                        sb.AppendLine("            </div>");
                    }
                    if (!isHidden("item_school_reg")) {
                        sb.AppendLine("            <div class=\"dd-school-item\">");
                        sb.AppendLine("              <a href=\"/Courses/Public?school=regulatory\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-scale-balanced\"></i></span><span class=\"dd-text\"><strong>School of Regulatory Services</strong><span>Aviation Safety &amp; Regulatory Compliance</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>");
                        sb.AppendLine("              <div class=\"dd-submenu\" data-school=\"regulatory\"></div>");
                        sb.AppendLine("            </div>");
                    }
                    if (!isHidden("item_school_apt")) {
                        sb.AppendLine("            <div class=\"dd-school-item\">");
                        sb.AppendLine("              <a href=\"/Courses/Public?school=airport\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-fire-extinguisher\"></i></span><span class=\"dd-text\"><strong>School of Airport Services</strong><span>Ground Operations &amp; Rescue/Firefighting</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>");
                        sb.AppendLine("              <div class=\"dd-submenu\" data-school=\"airport\"></div>");
                        sb.AppendLine("            </div>");
                    }
                    if (!isHidden("item_school_mgmt")) {
                        sb.AppendLine("            <div class=\"dd-school-item\">");
                        sb.AppendLine("              <a href=\"/Courses/Public?school=management\" class=\"dd-link dd-school-link\"><span class=\"dd-icon\"><i class=\"fas fa-briefcase\"></i></span><span class=\"dd-text\"><strong>School of Aviation Management</strong><span>Leadership &amp; Management Training</span></span><i class=\"fas fa-chevron-right dd-school-arrow\" onclick=\"event.preventDefault(); event.stopPropagation(); toggleSchoolSubmenu(this);\"></i></a>");
                        sb.AppendLine("              <div class=\"dd-submenu\" data-school=\"management\"></div>");
                        sb.AppendLine("            </div>");
                    }
                    if (!isHidden("item_school_viewall")) sb.AppendLine("            <a href=\"/Courses/Public\" class=\"dd-view-all\">View All Schools <i class=\"fas fa-arrow-right\"></i></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_academics_resources"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Student Resources</div>");
                    if (!isHidden("item_res_portal")) sb.AppendLine("            <a href=\"/Account/Login\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-user-circle\"></i></span><span class=\"dd-text\"><strong>Online Portal</strong><span>Access results, schedules &amp; fee challan</span></span></a>");
                    if (!isHidden("item_res_results")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openResults(event)\"><span class=\"dd-icon\"><i class=\"fas fa-poll\"></i></span><span class=\"dd-text\"><strong>Results</strong><span>Check your exam results</span></span></a>");
                    if (!isHidden("item_res_calendar")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openCalendar(event)\"><span class=\"dd-icon\"><i class=\"fas fa-calendar-alt\"></i></span><span class=\"dd-text\"><strong>Academic Calendar</strong><span>Semester dates and schedules</span></span></a>");
                    if (!isHidden("item_res_catalog")) sb.AppendLine("            <a href=\"/CourseCatalog\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-book-open\"></i></span><span class=\"dd-text\"><strong>Course Catalog</strong><span>All programs and syllabi</span></span></a>");
                    if (!isHidden("item_res_exams")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openExamSchedule(event)\"><span class=\"dd-icon\"><i class=\"fas fa-file-alt\"></i></span><span class=\"dd-text\"><strong>Examination Schedule</strong><span>Mid &amp; final term timetables</span></span></a>");
                    if (!isHidden("item_res_downloads")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openDownloads(event)\"><span class=\"dd-icon\"><i class=\"fas fa-download\"></i></span><span class=\"dd-text\"><strong>Downloads</strong><span>Forms, guides &amp; documents</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 2. ADMISSIONS
            if (!isHidden("menu_admissions"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/CourseCatalog\" class=\"nav-link\">Admissions <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_admissions_apply"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">How to Apply</div>");
                    if (!isHidden("item_adm_apply")) sb.AppendLine("            <a href=\"/Account/Register\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-paper-plane\"></i></span><span class=\"dd-text\"><strong>Apply Online</strong><span>Submit your application now</span></span></a>");
                    if (!isHidden("item_adm_eligibility")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openEligibility(event)\"><span class=\"dd-icon\"><i class=\"fas fa-list-check\"></i></span><span class=\"dd-text\"><strong>Eligibility Criteria</strong><span>Entry requirements per program</span></span></a>");
                    if (!isHidden("item_adm_docs")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openRequiredDocs(event)\"><span class=\"dd-icon\"><i class=\"fas fa-file-upload\"></i></span><span class=\"dd-text\"><strong>Required Documents</strong><span>Documents needed for admission</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_admissions_financial"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Financial</div>");
                    if (!isHidden("item_adm_fee")) sb.AppendLine("            <a href=\"/financial.html#fee-structure\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-coins\"></i></span><span class=\"dd-text\"><strong>Fee Structure</strong><span>Semester-wise fee breakdown</span></span></a>");
                    if (!isHidden("item_adm_scholarships")) sb.AppendLine("            <a href=\"/financial.html#scholarships\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-hand-holding-usd\"></i></span><span class=\"dd-text\"><strong>Scholarships</strong><span>Merit and need-based aid</span></span></a>");
                    if (!isHidden("item_adm_payment")) sb.AppendLine("            <a href=\"/financial.html#payment-methods\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-receipt\"></i></span><span class=\"dd-text\"><strong>Payment Methods</strong><span>Bank and online payment options</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("card_admissions_featured"))
                {
                    sb.AppendLine("          <div class=\"dd-col\" style=\"flex:0 0 220px;\">");
                    sb.AppendLine("            <a href=\"/Account/Register\" class=\"dd-featured\">");
                    sb.AppendLine("              <div class=\"feat-label\">Admissions Open 2026</div>");
                    sb.AppendLine("              <div class=\"feat-title\">Begin Your Aviation Career Today</div>");
                    sb.AppendLine("              <div class=\"feat-desc\">Applications are being accepted for Fall 2026. Limited seats available.</div>");
                    sb.AppendLine("              <span class=\"feat-btn\">Apply Now</span>");
                    sb.AppendLine("            </a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 3. TRAINING
            if (!isHidden("menu_training"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/SimulationLabs/Public\" class=\"nav-link\">Training <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_training_labs"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Simulation &amp; Labs</div>");
                    if (!isHidden("item_trn_simulators")) sb.AppendLine("            <a href=\"/SimulationLabs/Public#flight-simulators\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-plane\"></i></span><span class=\"dd-text\"><strong>Flight Simulators</strong><span>State-of-the-art FFS &amp; FTD devices</span></span></a>");
                    if (!isHidden("item_trn_atc")) sb.AppendLine("            <a href=\"/SimulationLabs/Public#atc-simulator\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-tower-broadcast\"></i></span><span class=\"dd-text\"><strong>ATC Simulator</strong><span>Live radar and voice simulation</span></span></a>");
                    if (!isHidden("item_trn_avionics")) sb.AppendLine("            <a href=\"/SimulationLabs/Public#avionics-lab\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-microchip\"></i></span><span class=\"dd-text\"><strong>Avionics Lab</strong><span>Practical avionics systems training</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_training_courses"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Professional Courses</div>");
                    if (!isHidden("item_trn_pcaa")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openProfCourses(event,'pcaa')\"><span class=\"dd-icon\"><i class=\"fas fa-user-graduate\"></i></span><span class=\"dd-text\"><strong>PCAA Type Ratings</strong><span>Endorsed by Civil Aviation Authority</span></span></a>");
                    if (!isHidden("item_trn_icao")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openProfCourses(event,'icao')\"><span class=\"dd-icon\"><i class=\"fas fa-globe\"></i></span><span class=\"dd-text\"><strong>ICAO Courses</strong><span>Internationally recognized programs</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 4. CAMPUS LIFE
            if (!isHidden("menu_campus_life"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/facilities.html\" class=\"nav-link\">Campus Life <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_campus_facilities"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Facilities</div>");
                    if (!isHidden("item_fac_library")) sb.AppendLine("            <a href=\"/facility-library.html\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-book\"></i></span><span class=\"dd-text\"><strong>Library &amp; Resource Center</strong><span>Digital and physical collections</span></span></a>");
                    if (!isHidden("item_fac_cafeteria")) sb.AppendLine("            <a href=\"/facility-cafeteria.html\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-utensils\"></i></span><span class=\"dd-text\"><strong>Cafeteria &amp; Dining</strong><span>On-campus food services</span></span></a>");
                    if (!isHidden("item_fac_hostel")) sb.AppendLine("            <a href=\"/facility-hostel.html\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-bed\"></i></span><span class=\"dd-text\"><strong>Hostel</strong><span>On-campus residential accommodation</span></span></a>");
                    if (!isHidden("item_fac_mess")) sb.AppendLine("            <a href=\"/facility-mess.html\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-utensils\"></i></span><span class=\"dd-text\"><strong>Mess</strong><span>Student dining and meal services</span></span></a>");
                    if (!isHidden("item_fac_viewall_facilities")) sb.AppendLine("            <a href=\"/facilities.html\" class=\"dd-view-all\">View All Facilities <i class=\"fas fa-arrow-right\"></i></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_campus_studentlife"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Student Life</div>");
                    if (!isHidden("item_life_events")) sb.AppendLine("            <a href=\"#\" class=\"dd-link\" onclick=\"openEvents(event)\"><span class=\"dd-icon\"><i class=\"fas fa-calendar-check\"></i></span><span class=\"dd-text\"><strong>Events &amp; Activities</strong><span>Seminars, trips and cultural days</span></span></a>");
                    if (!isHidden("item_life_career")) sb.AppendLine("            <a href=\"https://paa.gov.pk/media/job-opportunities\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-briefcase\"></i></span><span class=\"dd-text\"><strong>Career Services</strong><span>Placement and internship support</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_campus_news"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">News &amp; Media</div>");
                    if (!isHidden("item_news_latest")) sb.AppendLine("            <a href=\"/NewsPosts/Public#latest-news\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-newspaper\"></i></span><span class=\"dd-text\"><strong>Latest News</strong><span>University announcements</span></span></a>");
                    if (!isHidden("item_news_photos")) sb.AppendLine("            <a href=\"/NewsPosts/Public#photo-gallery\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-images\"></i></span><span class=\"dd-text\"><strong>Photo Gallery</strong><span>Campus life in pictures</span></span></a>");
                    if (!isHidden("item_news_videos")) sb.AppendLine("            <a href=\"/NewsPosts/Public#video-gallery\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-video\"></i></span><span class=\"dd-text\"><strong>Video Gallery</strong><span>Events and training videos</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 5. USEFUL LINKS
            if (!isHidden("menu_useful_links"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/links.html\" class=\"nav-link\">Useful Links <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_useful_links"))
                {
                    sb.AppendLine("          <div class=\"dd-col\" style=\"flex:0 0 auto;min-width:260px;\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Useful links</div>");
                    if (!isHidden("item_link_lms")) sb.AppendLine("            <a href=\"https://cati-lms.pitb.gov.pk/\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-flag\"></i></span><span class=\"dd-text\"><strong>LMS</strong></span></a>");
                    if (!isHidden("item_link_elibrary")) sb.AppendLine("            <a href=\"https://cati-elibrary.pitb.gov.pk/\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-landmark\"></i></span><span class=\"dd-text\"><strong>E-library</strong></span></a>");
                    if (!isHidden("item_link_viewall")) sb.AppendLine("            <a href=\"/links.html\" class=\"dd-view-all\">View All Links <i class=\"fas fa-arrow-right\"></i></a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 6. ABOUT
            if (!isHidden("menu_about"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/about.html\" class=\"nav-link\">About <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_about_links"))
                {
                    sb.AppendLine("          <div class=\"dd-col\" style=\"flex:0 0 auto;min-width:260px;\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">About</div>");
                    if (!isHidden("item_abt_history_caa")) sb.AppendLine("            <a href=\"/about.html#history-caa\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-flag\"></i></span><span class=\"dd-text\"><strong>History of CAA in Pakistan</strong><span>Evolution of Civil Aviation Authority</span></span></a>");
                    if (!isHidden("item_abt_history_cati")) sb.AppendLine("            <a href=\"/about.html#history-cati\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-landmark\"></i></span><span class=\"dd-text\"><strong>History of CATI</strong><span>Our journey and founding milestones</span></span></a>");
                    if (!isHidden("item_abt_mgmt")) sb.AppendLine("            <a href=\"/about.html#management\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-sitemap\"></i></span><span class=\"dd-text\"><strong>Management</strong><span>Leadership and organizational structure</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("card_about_featured"))
                {
                    sb.AppendLine("          <div class=\"dd-col\" style=\"flex:0 0 220px;margin-left:auto;\">");
                    sb.AppendLine("            <a href=\"/about.html\" class=\"dd-featured\">");
                    sb.AppendLine("              <div class=\"feat-label\">Established 2026</div>");
                    sb.AppendLine("              <div class=\"feat-title\">Pakistan's Premier Aviation Institution</div>");
                    sb.AppendLine("              <div class=\"feat-desc\">Located at Airport Road, Hyderabad — shaping future aviation leaders.</div>");
                    sb.AppendLine("              <span class=\"feat-btn\">Learn More</span>");
                    sb.AppendLine("            </a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // 7. CONTACT
            if (!isHidden("menu_contact"))
            {
                sb.AppendLine("    <div class=\"menu-item\">");
                sb.AppendLine("      <a href=\"/Contact\" class=\"nav-link\">Contact <i class=\"fas fa-chevron-down chevron\"></i></a>");
                sb.AppendLine("      <div class=\"dropdown\">");
                sb.AppendLine("        <div class=\"dd-inner\">");

                if (!isHidden("col_contact_info"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Get In Touch</div>");
                    if (!isHidden("item_cnt_visit")) sb.AppendLine("            <a href=\"/Contact\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-map-marker-alt\"></i></span><span class=\"dd-text\"><strong>Visit Us</strong><span>Airport Road, Hyderabad, Sindh</span></span></a>");
                    if (!isHidden("item_cnt_call")) sb.AppendLine("            <a href=\"tel:+92229260310\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-phone\"></i></span><span class=\"dd-text\"><strong>Call Us</strong><span>+92 22 9260310-19</span></span></a>");
                    if (!isHidden("item_cnt_email")) sb.AppendLine("            <a href=\"mailto:AdID.Academics@paa.gov.pk\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-envelope\"></i></span><span class=\"dd-text\"><strong>Email Us</strong><span>AdID.Academics@paa.gov.pk</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("col_contact_depts"))
                {
                    sb.AppendLine("          <div class=\"dd-col\">");
                    sb.AppendLine("            <div class=\"dd-col-title\">Departments</div>");
                    if (!isHidden("item_cnt_adm_office")) sb.AppendLine("            <a href=\"mailto:admissions@au.edu.pk\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-user-graduate\"></i></span><span class=\"dd-text\"><strong>Admissions Office</strong><span>admissions@au.edu.pk</span></span></a>");
                    if (!isHidden("item_cnt_res_office")) sb.AppendLine("            <a href=\"mailto:research@au.edu.pk\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-microscope\"></i></span><span class=\"dd-text\"><strong>Research Office</strong><span>research@au.edu.pk</span></span></a>");
                    if (!isHidden("item_cnt_pr_office")) sb.AppendLine("            <a href=\"mailto:pr@au.edu.pk\" class=\"dd-link\"><span class=\"dd-icon\"><i class=\"fas fa-comments\"></i></span><span class=\"dd-text\"><strong>Public Relations</strong><span>pr@au.edu.pk</span></span></a>");
                    sb.AppendLine("          </div>");
                }

                if (!isHidden("card_contact_featured"))
                {
                    sb.AppendLine("          <div class=\"dd-col\" style=\"flex:0 0 220px;\">");
                    sb.AppendLine("            <a href=\"/Account/Login\" class=\"dd-featured\">");
                    sb.AppendLine("              <div class=\"feat-label\">Student Portal</div>");
                    sb.AppendLine("              <div class=\"feat-title\">Access Your Online Portal</div>");
                    sb.AppendLine("              <div class=\"feat-desc\">Check results, schedules, fee challan and more from anywhere.</div>");
                    sb.AppendLine("              <span class=\"feat-btn\">Sign In</span>");
                    sb.AppendLine("            </a>");
                    sb.AppendLine("          </div>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }

            // SEARCH
            sb.AppendLine("    <div class=\"search-wrap\">");
            sb.AppendLine("      <i class=\"fas fa-search search-icon\" onclick=\"toggleSearch(event)\"></i>");
            sb.AppendLine("      <input id=\"searchBox\" class=\"search-box\" placeholder=\"Search...\" onblur=\"closeSearch()\">");
            sb.AppendLine("    </div>");

            sb.AppendLine("  </div>");
            sb.AppendLine("</nav>");

            return sb.ToString();
        }

        public static List<NavMenuSetting> GetDefaultMenuHierarchy()
        {
            int order = 1;
            return new List<NavMenuSetting>
            {
                // 1. ACADEMICS
                new() { MenuKey = "menu_academics", Title = "Academics", Category = "Menu", DisplayOrder = order++, Description = "Top-level Academics menu" },
                new() { MenuKey = "col_academics_faculties", ParentKey = "menu_academics", Title = "Faculties", Category = "Column", DisplayOrder = order++, Description = "Faculties list column in Academics" },
                new() { MenuKey = "item_fac_engineering", ParentKey = "col_academics_faculties", Title = "Faculty of Engineering", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_it", ParentKey = "col_academics_faculties", Title = "Faculty of IT & Cyber Security", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_operations", ParentKey = "col_academics_faculties", Title = "Faculty of Aviation Operations", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_management", ParentKey = "col_academics_faculties", Title = "Faculty of Management Sciences", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_viewall", ParentKey = "col_academics_faculties", Title = "View All Faculties Link", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_academics_schools", ParentKey = "menu_academics", Title = "Schools", Category = "Column", DisplayOrder = order++, Description = "Schools list column in Academics" },
                new() { MenuKey = "item_school_ans", ParentKey = "col_academics_schools", Title = "School of Air Navigation Services (ANS)", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_school_reg", ParentKey = "col_academics_schools", Title = "School of Regulatory Services", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_school_apt", ParentKey = "col_academics_schools", Title = "School of Airport Services", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_school_mgmt", ParentKey = "col_academics_schools", Title = "School of Aviation Management", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_school_viewall", ParentKey = "col_academics_schools", Title = "View All Schools Link", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_academics_resources", ParentKey = "menu_academics", Title = "Student Resources", Category = "Column", DisplayOrder = order++, Description = "Student Resources column in Academics" },
                new() { MenuKey = "item_res_portal", ParentKey = "col_academics_resources", Title = "Online Portal", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_res_results", ParentKey = "col_academics_resources", Title = "Results", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_res_calendar", ParentKey = "col_academics_resources", Title = "Academic Calendar", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_res_catalog", ParentKey = "col_academics_resources", Title = "Course Catalog", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_res_exams", ParentKey = "col_academics_resources", Title = "Examination Schedule", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_res_downloads", ParentKey = "col_academics_resources", Title = "Downloads", Category = "Link", DisplayOrder = order++ },

                // 2. ADMISSIONS
                new() { MenuKey = "menu_admissions", Title = "Admissions", Category = "Menu", DisplayOrder = order++, Description = "Top-level Admissions menu" },
                new() { MenuKey = "col_admissions_apply", ParentKey = "menu_admissions", Title = "How to Apply", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_apply", ParentKey = "col_admissions_apply", Title = "Apply Online", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_eligibility", ParentKey = "col_admissions_apply", Title = "Eligibility Criteria", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_docs", ParentKey = "col_admissions_apply", Title = "Required Documents", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_admissions_financial", ParentKey = "menu_admissions", Title = "Financial", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_fee", ParentKey = "col_admissions_financial", Title = "Fee Structure", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_scholarships", ParentKey = "col_admissions_financial", Title = "Scholarships", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_adm_payment", ParentKey = "col_admissions_financial", Title = "Payment Methods", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "card_admissions_featured", ParentKey = "menu_admissions", Title = "Admissions Featured Card (Apply Now)", Category = "Card", DisplayOrder = order++ },

                // 3. TRAINING
                new() { MenuKey = "menu_training", Title = "Training", Category = "Menu", DisplayOrder = order++, Description = "Top-level Training menu" },
                new() { MenuKey = "col_training_labs", ParentKey = "menu_training", Title = "Simulation & Labs", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_trn_simulators", ParentKey = "col_training_labs", Title = "Flight Simulators", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_trn_atc", ParentKey = "col_training_labs", Title = "ATC Simulator", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_trn_avionics", ParentKey = "col_training_labs", Title = "Avionics Lab", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_training_courses", ParentKey = "menu_training", Title = "Professional Courses", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_trn_pcaa", ParentKey = "col_training_courses", Title = "PCAA Type Ratings", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_trn_icao", ParentKey = "col_training_courses", Title = "ICAO Courses", Category = "Link", DisplayOrder = order++ },

                // 4. CAMPUS LIFE
                new() { MenuKey = "menu_campus_life", Title = "Campus Life", Category = "Menu", DisplayOrder = order++, Description = "Top-level Campus Life menu" },
                new() { MenuKey = "col_campus_facilities", ParentKey = "menu_campus_life", Title = "Facilities", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_library", ParentKey = "col_campus_facilities", Title = "Library & Resource Center", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_cafeteria", ParentKey = "col_campus_facilities", Title = "Cafeteria & Dining", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_hostel", ParentKey = "col_campus_facilities", Title = "Hostel", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_mess", ParentKey = "col_campus_facilities", Title = "Mess", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_fac_viewall_facilities", ParentKey = "col_campus_facilities", Title = "View All Facilities Link", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_campus_studentlife", ParentKey = "menu_campus_life", Title = "Student Life", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_life_events", ParentKey = "col_campus_studentlife", Title = "Events & Activities", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_life_career", ParentKey = "col_campus_studentlife", Title = "Career Services", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_campus_news", ParentKey = "menu_campus_life", Title = "News & Media", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_news_latest", ParentKey = "col_campus_news", Title = "Latest News", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_news_photos", ParentKey = "col_campus_news", Title = "Photo Gallery", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_news_videos", ParentKey = "col_campus_news", Title = "Video Gallery", Category = "Link", DisplayOrder = order++ },

                // 5. USEFUL LINKS
                new() { MenuKey = "menu_useful_links", Title = "Useful Links", Category = "Menu", DisplayOrder = order++, Description = "Top-level Useful Links menu" },
                new() { MenuKey = "col_useful_links", ParentKey = "menu_useful_links", Title = "Useful links Column", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_link_lms", ParentKey = "col_useful_links", Title = "LMS", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_link_elibrary", ParentKey = "col_useful_links", Title = "E-library", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_link_viewall", ParentKey = "col_useful_links", Title = "View All Links", Category = "Link", DisplayOrder = order++ },

                // 6. ABOUT
                new() { MenuKey = "menu_about", Title = "About", Category = "Menu", DisplayOrder = order++, Description = "Top-level About menu" },
                new() { MenuKey = "col_about_links", ParentKey = "menu_about", Title = "About Links Column", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_abt_history_caa", ParentKey = "col_about_links", Title = "History of CAA in Pakistan", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_abt_history_cati", ParentKey = "col_about_links", Title = "History of CATI", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_abt_mgmt", ParentKey = "col_about_links", Title = "Management", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "card_about_featured", ParentKey = "menu_about", Title = "About Featured Card (Established 2026)", Category = "Card", DisplayOrder = order++ },

                // 7. CONTACT
                new() { MenuKey = "menu_contact", Title = "Contact Us", Category = "Menu", DisplayOrder = order++, Description = "Top-level Contact menu" },
                new() { MenuKey = "col_contact_info", ParentKey = "menu_contact", Title = "Get In Touch", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_visit", ParentKey = "col_contact_info", Title = "Visit Us", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_call", ParentKey = "col_contact_info", Title = "Call Us", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_email", ParentKey = "col_contact_info", Title = "Email Us", Category = "Link", DisplayOrder = order++ },

                new() { MenuKey = "col_contact_depts", ParentKey = "menu_contact", Title = "Departments", Category = "Column", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_adm_office", ParentKey = "col_contact_depts", Title = "Admissions Office", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_res_office", ParentKey = "col_contact_depts", Title = "Research Office", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "item_cnt_pr_office", ParentKey = "col_contact_depts", Title = "Public Relations", Category = "Link", DisplayOrder = order++ },
                new() { MenuKey = "card_contact_featured", ParentKey = "menu_contact", Title = "Contact Featured Card (Student Portal)", Category = "Card", DisplayOrder = order++ }
            };
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;
using NAU_Web.Services;
using System.Text.Json;

namespace NAU_Web.Controllers
{
    public class FinancialController : Controller
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserPermissionService _permissionService;

        public const string FinancialSlug = "financial";

        public FinancialController(
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

        // GET: /Financial or /financial.html
        [AllowAnonymous]
        [HttpGet("/Financial")]
        [HttpGet("/financial.html")]
        public async Task<IActionResult> Index(string? edit = null)
        {
            bool wantsEdit = !string.IsNullOrEmpty(edit) &&
                (edit == "1" || edit.Equals("true", StringComparison.OrdinalIgnoreCase) || edit.Equals("yes", StringComparison.OrdinalIgnoreCase));

            ApplicationUser? currentUser = null;
            bool canEdit = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    canEdit = await _permissionService.HasPermissionAsync(currentUser.Id, AppPermissions.ContentFinancial)
                           || await _permissionService.IsSuperAdminAsync(currentUser.Id);
                }
            }

            ViewBag.EditMode = wantsEdit && canEdit;

            ViewBag.CurrentUserName = "";
            if (currentUser != null)
            {
                ViewBag.CurrentUserName = !string.IsNullOrWhiteSpace(currentUser.FullName)
                    ? currentUser.FullName
                    : (User.Identity?.Name ?? "");
            }

            var content = await LoadOrSeedFinancialContentAsync();
            return View(content);
        }

        private async Task<FinancialPageContent> LoadOrSeedFinancialContentAsync()
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == FinancialSlug);
            FinancialPageContent? data = null;

            if (page != null && !string.IsNullOrWhiteSpace(page.ContentHtml))
            {
                try
                {
                    data = JsonSerializer.Deserialize<FinancialPageContent>(page.ContentHtml);
                }
                catch
                {
                    data = null;
                }
            }

            if (data == null || data.FeeItems == null || !data.FeeItems.Any())
            {
                data = GetDefaultFinancialData();

                if (page == null)
                {
                    page = new Page
                    {
                        Slug = FinancialSlug,
                        Title = "Financial Information - National Aviation University",
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
            else if (page != null)
            {
                RecalculateAllTotals(data);
                SyncAllFeeCards(data);
                EnsureBankLogos(data);
                page.ContentHtml = JsonSerializer.Serialize(data);
                await _context.SaveChangesAsync();
            }

            return data;
        }

        private void EnsureBankLogos(FinancialPageContent? data)
        {
            if (data == null || data.Banks == null) return;

            // 1. If there's an old combined "Easypaisa / JazzCash" entry, clean it to just "Easypaisa"
            var combined = data.Banks.FirstOrDefault(b => (b.Name ?? "").Contains("Easypaisa", StringComparison.OrdinalIgnoreCase) && (b.Name ?? "").Contains("Jazz", StringComparison.OrdinalIgnoreCase));
            if (combined != null)
            {
                combined.Name = "Easypaisa";
                combined.Details = "Mobile wallet payment via challan number & Easypaisa App";
                combined.TagText = "Mobile";
                combined.TagBg = "#e6f4ea";
                combined.TagColor = "#1e7e34";
                combined.LogoText = "EP";
                combined.LogoBgHex = "#00a859";
                combined.LogoUrl = "/images/banks/easypaisa.png";
            }

            // 2. Ensure JazzCash exists as its own separate bank entry
            if (!data.Banks.Any(b => (b.Name ?? "").Equals("JazzCash", StringComparison.OrdinalIgnoreCase) || (b.Name ?? "").StartsWith("Jazz", StringComparison.OrdinalIgnoreCase)))
            {
                data.Banks.Add(new BankData
                {
                    Id = "bank-6",
                    Name = "JazzCash",
                    Details = "Mobile wallet payment & retail shop agents",
                    TagText = "Mobile",
                    TagBg = "#fff7e6",
                    TagColor = "#c87800",
                    LogoText = "JAZZ",
                    LogoBgHex = "#d81e05",
                    LogoUrl = "/images/banks/jazzcash.png"
                });
            }

            // 3. Ensure all banks have their correct logo URLs
            foreach (var b in data.Banks)
            {
                var n = (b.Name ?? "").ToLowerInvariant();
                var l = (b.LogoText ?? "").ToLowerInvariant();
                if (n.Contains("hbl") || n.Contains("habib") || l == "hbl")
                {
                    b.LogoUrl = "/images/banks/hbl.png";
                    b.LogoBgHex = "#006b52";
                }
                else if (n.Contains("nbp") || n.Contains("national bank") || l == "nbp")
                {
                    b.LogoUrl = "/images/banks/nbp.png";
                    b.LogoBgHex = "#006039";
                }
                else if (n.Contains("mcb") || l == "mcb")
                {
                    b.LogoUrl = "/images/banks/mcb.png";
                    b.LogoBgHex = "#ffffff";
                }
                else if (n.Contains("ubl") || n.Contains("united bank") || l == "ubl")
                {
                    b.LogoUrl = "/images/banks/ubl.png";
                    b.LogoBgHex = "#005baa";
                }
                else if (n.Contains("easypaisa") || l == "easy" || l == "ep")
                {
                    b.Name = "Easypaisa";
                    b.LogoUrl = "/images/banks/easypaisa.png";
                    b.LogoBgHex = "#00a859";
                }
                else if (n.Contains("jazz") || l == "jazz")
                {
                    b.Name = "JazzCash";
                    b.LogoUrl = "/images/banks/jazzcash.png";
                    b.LogoBgHex = "#d81e05";
                }
            }
        }

        private void RecalculateTotalsForCategory(FinancialPageContent content, string category)
        {
            if (content == null || content.FeeItems == null) return;
            if (category == "short") return; // Short courses are range-based, don't sum

            var items = content.FeeItems.Where(i => i.Category == category).OrderBy(i => i.DisplayOrder).ToList();
            
            long semesterSum = 0;
            long oneTimeSum = 0;

            foreach (var item in items)
            {
                var comp = (item.Component ?? "").Trim();
                var freq = (item.Frequency ?? "").Trim();

                bool isSub = item.IsSubtotal || comp.Equals("Semester Total", StringComparison.OrdinalIgnoreCase) || comp.Contains("Semester Total", StringComparison.OrdinalIgnoreCase) || comp.Contains("Subtotal", StringComparison.OrdinalIgnoreCase);
                bool isTot = item.IsTotal || comp.StartsWith("Estimated", StringComparison.OrdinalIgnoreCase) || comp.Contains("Program Cost", StringComparison.OrdinalIgnoreCase);

                if (isSub) { item.IsSubtotal = true; continue; }
                if (isTot) { item.IsTotal = true; continue; }
                
                bool isOneTime = freq.Contains("One-Time", StringComparison.OrdinalIgnoreCase) || 
                                 comp.Contains("Registration", StringComparison.OrdinalIgnoreCase);
                
                var digits = new string((item.Amount ?? "").Where(char.IsDigit).ToArray());
                if (long.TryParse(digits, out long val))
                {
                    if (isOneTime)
                    {
                        oneTimeSum += val;
                    }
                    else
                    {
                        semesterSum += val;
                    }
                }
            }

            var subItem = items.FirstOrDefault(i => i.IsSubtotal || (i.Component ?? "").Contains("Semester Total", StringComparison.OrdinalIgnoreCase));
            if (subItem != null)
            {
                subItem.IsSubtotal = true;
                subItem.Amount = $"{semesterSum:N0}";
            }

            var totItem = items.FirstOrDefault(i => i.IsTotal || (i.Component ?? "").Contains("Estimated", StringComparison.OrdinalIgnoreCase));
            if (totItem != null)
            {
                totItem.IsTotal = true;
                int multiplier = 8;
                if (category == "mba") multiplier = 4;
                else if (category == "short") multiplier = 1;

                var freqDigits = new string((totItem.Frequency ?? "").Where(char.IsDigit).ToArray());
                if (int.TryParse(freqDigits, out int parsedMult) && parsedMult > 0)
                {
                    multiplier = parsedMult;
                }

                long estTotal = (semesterSum * multiplier) + oneTimeSum;
                totItem.Amount = $"{estTotal:N0}";
            }
        }

        private void RecalculateAllTotals(FinancialPageContent content)
        {
            if (content == null) return;
            foreach (var cat in new[] { "be", "bs", "mba", "short" })
            {
                RecalculateTotalsForCategory(content, cat);
            }
        }

        private void SyncFeeCardsForCategory(FinancialPageContent content, string category)
        {
            if (content == null || content.FeeItems == null || content.FeeCards == null) return;

            var items = content.FeeItems.Where(i => i.Category == category).ToList();
            var cards = content.FeeCards.Where(c => c.Category == category).ToList();

            var regItem = items.FirstOrDefault(i => i.Component.Contains("Registration", StringComparison.OrdinalIgnoreCase) || 
                                                   i.Frequency.Contains("One-Time", StringComparison.OrdinalIgnoreCase));
            if (regItem != null)
            {
                var regCard = cards.FirstOrDefault(c => c.Title.Contains("Registration", StringComparison.OrdinalIgnoreCase));
                if (regCard != null)
                {
                    var digits = new string((regItem.Amount ?? "").Where(char.IsDigit).ToArray());
                    if (long.TryParse(digits, out long val))
                    {
                        regCard.Amount = $"PKR {val:N0}";
                    }
                }
            }

            var subItem = items.FirstOrDefault(i => i.IsSubtotal || i.Component.Contains("Semester Total", StringComparison.OrdinalIgnoreCase));
            if (subItem != null)
            {
                var semCard = cards.FirstOrDefault(c => c.Title.Equals("Per Semester", StringComparison.OrdinalIgnoreCase));
                if (semCard != null)
                {
                    var digits = new string((subItem.Amount ?? "").Where(char.IsDigit).ToArray());
                    if (long.TryParse(digits, out long val))
                    {
                        semCard.Amount = $"PKR {val:N0}";
                    }
                }

                var annCard = cards.FirstOrDefault(c => c.Title.Equals("Annual Cost", StringComparison.OrdinalIgnoreCase));
                if (annCard != null)
                {
                    var digits = new string((subItem.Amount ?? "").Where(char.IsDigit).ToArray());
                    if (long.TryParse(digits, out long val))
                    {
                        annCard.Amount = $"PKR {val * 2:N0}";
                    }
                }
            }
        }

        private void SyncAllFeeCards(FinancialPageContent content)
        {
            if (content == null) return;
            foreach (var cat in new[] { "be", "bs", "mba", "short" })
            {
                SyncFeeCardsForCategory(content, cat);
            }
        }

        private async Task SaveFinancialContentAsync(FinancialPageContent data)
        {
            var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == FinancialSlug);
            var user = await _userManager.GetUserAsync(User);

            if (page == null)
            {
                page = new Page
                {
                    Slug = FinancialSlug,
                    Title = "Financial Information - National Aviation University",
                    ContentHtml = JsonSerializer.Serialize(data),
                    IsPublished = true,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = user?.Id
                };
                _context.Pages.Add(page);
            }
            else
            {
                page.ContentHtml = JsonSerializer.Serialize(data);
                page.UpdatedAt = DateTime.UtcNow;
                page.UpdatedByUserId = user?.Id;
            }

            await _context.SaveChangesAsync();
        }

        // ================= ADMIN AJAX ENDPOINTS (Schools & SimLabs Pattern) =================

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveFeeItem([FromBody] FeeItemData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Component))
                return Json(new { success = false, message = "Fee component title is required." });

            var content = await LoadOrSeedFinancialContentAsync();
            var existing = content.FeeItems.FirstOrDefault(f => f.Id == model.Id);

            if (existing != null)
            {
                existing.Category = model.Category;
                existing.Component = model.Component;
                existing.Amount = model.Amount;
                existing.Frequency = model.Frequency;
                existing.Note = model.Note;
                if (model.IsSubtotal) existing.IsSubtotal = true;
                if (model.IsTotal) existing.IsTotal = true;
            }
            else
            {
                if (string.IsNullOrEmpty(model.Id)) model.Id = Guid.NewGuid().ToString("N");
                content.FeeItems.Add(model);
            }

            RecalculateTotalsForCategory(content, model.Category);
            SyncFeeCardsForCategory(content, model.Category);
            await SaveFinancialContentAsync(content);
            return Json(new { success = true, item = model });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteFeeItem([FromBody] FeeItemData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
                return Json(new { success = false, message = "Invalid fee item id." });

            var content = await LoadOrSeedFinancialContentAsync();
            content.FeeItems.RemoveAll(f => f.Id == model.Id);

            if (!string.IsNullOrEmpty(model.Category))
            {
                RecalculateTotalsForCategory(content, model.Category);
                SyncFeeCardsForCategory(content, model.Category);
            }
            else
            {
                RecalculateAllTotals(content);
                SyncAllFeeCards(content);
            }

            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveFeeCard([FromBody] FeeCardData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
                return Json(new { success = false, message = "Invalid fee card id." });

            var content = await LoadOrSeedFinancialContentAsync();
            var existing = content.FeeCards.FirstOrDefault(c => c.Id == model.Id);

            if (existing != null)
            {
                existing.Title = model.Title;
                existing.Amount = model.Amount;
                existing.Period = model.Period;
                existing.Note = model.Note;
            }

            await SaveFinancialContentAsync(content);
            return Json(new { success = true, item = existing });
        }

        public class FeeHeaderModel
        {
            public string Label { get; set; } = "";
            public string Title { get; set; } = "";
            public string Desc { get; set; } = "";
            public string Notice { get; set; } = "";
        }

        public class ProgramTabsModel
        {
            public string Be { get; set; } = "";
            public string Bs { get; set; } = "";
            public string Mba { get; set; } = "";
            public string Short { get; set; } = "";
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveFeeHeader([FromBody] FeeHeaderModel model)
        {
            var content = await LoadOrSeedFinancialContentAsync();
            content.FeeSectionLabel = model.Label;
            content.FeeSectionTitle = model.Title;
            content.FeeSectionDesc = model.Desc;
            content.FeeNoticeText = model.Notice;
            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveProgramTabs([FromBody] ProgramTabsModel model)
        {
            var content = await LoadOrSeedFinancialContentAsync();
            content.TabLabelBe = model.Be;
            content.TabLabelBs = model.Bs;
            content.TabLabelMba = model.Mba;
            content.TabLabelShort = model.Short;
            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        public class SectionHeaderModel
        {
            public string Label { get; set; } = "";
            public string Title { get; set; } = "";
            public string Desc { get; set; } = "";
            public string Notice { get; set; } = "";
            public string SubTitle { get; set; } = "";
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveScholarshipHeader([FromBody] SectionHeaderModel model)
        {
            var content = await LoadOrSeedFinancialContentAsync();
            content.ScholarshipSectionLabel = model.Label;
            content.ScholarshipSectionTitle = model.Title;
            content.ScholarshipSectionDesc = model.Desc;
            content.ScholarshipNoticeText = model.Notice;
            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SavePaymentHeader([FromBody] SectionHeaderModel model)
        {
            var content = await LoadOrSeedFinancialContentAsync();
            content.PaymentSectionLabel = model.Label;
            content.PaymentSectionTitle = model.Title;
            content.PaymentSectionDesc = model.Desc;
            content.PaymentSubTitle = model.SubTitle;
            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveScholarship([FromBody] ScholarshipData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title))
                return Json(new { success = false, message = "Scholarship title is required." });

            var content = await LoadOrSeedFinancialContentAsync();
            var existing = content.Scholarships.FirstOrDefault(s => s.Id == model.Id);

            if (existing != null)
            {
                existing.Title = model.Title;
                existing.Subtitle = model.Subtitle;
                existing.Percentage = model.Percentage;
                existing.PercentageLabel = model.PercentageLabel;
                existing.Criteria = model.Criteria ?? new List<string>();
                existing.BadgeText = model.BadgeText;
                existing.BadgeBg = model.BadgeBg;
                existing.BadgeColor = model.BadgeColor;
                existing.HeaderGradient = model.HeaderGradient;
            }
            else
            {
                if (string.IsNullOrEmpty(model.Id)) model.Id = Guid.NewGuid().ToString("N");
                content.Scholarships.Add(model);
            }

            await SaveFinancialContentAsync(content);
            return Json(new { success = true, item = model });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteScholarship([FromBody] ScholarshipData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
                return Json(new { success = false, message = "Invalid scholarship id." });

            var content = await LoadOrSeedFinancialContentAsync();
            content.Scholarships.RemoveAll(s => s.Id == model.Id);

            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> SaveBank([FromBody] BankData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Bank name is required." });

            var content = await LoadOrSeedFinancialContentAsync();
            var existing = content.Banks.FirstOrDefault(b => b.Id == model.Id);

            if (existing != null)
            {
                existing.Name = model.Name;
                existing.Details = model.Details;
                existing.TagText = model.TagText;
                existing.TagBg = model.TagBg;
                existing.TagColor = model.TagColor;
                existing.LogoText = model.LogoText;
                existing.LogoBgHex = model.LogoBgHex;
                existing.LogoUrl = model.LogoUrl ?? "";
            }
            else
            {
                if (string.IsNullOrEmpty(model.Id)) model.Id = Guid.NewGuid().ToString("N");
                content.Banks.Add(model);
            }

            EnsureBankLogos(content);
            await SaveFinancialContentAsync(content);
            return Json(new { success = true, item = model });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteBank([FromBody] BankData model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Id))
                return Json(new { success = false, message = "Invalid bank id." });

            var content = await LoadOrSeedFinancialContentAsync();
            content.Banks.RemoveAll(b => b.Id == model.Id);

            await SaveFinancialContentAsync(content);
            return Json(new { success = true });
        }

        // Default seed data for Financial page
        private FinancialPageContent GetDefaultFinancialData()
        {
            return new FinancialPageContent
            {
                FeeNoticeText = "Fees must be paid within the first two weeks of each semester. Late payment may attract a surcharge of PKR 500 per week. CAA employees and nominees may be covered under their respective departmental training budgets.",
                ScholarshipNoticeText = "Scholarship applications must be submitted within the first three weeks of each semester. Late applications will not be entertained. Maintaining the scholarship requires fulfilling academic and conduct requirements each semester.",
                FeeItems = new List<FeeItemData>
                {
                    // BE
                    new FeeItemData { Id = "be-1", Category = "be", Component = "Tuition Fee", Amount = "45,000", Frequency = "Per Semester", Note = "BE ANS / Airport Engineering", DisplayOrder = 1 },
                    new FeeItemData { Id = "be-2", Category = "be", Component = "Registration Fee", Amount = "5,000", Frequency = "One-Time", Note = "At time of admission", DisplayOrder = 2 },
                    new FeeItemData { Id = "be-3", Category = "be", Component = "Examination Fee", Amount = "3,000", Frequency = "Per Semester", Note = "Includes mid & final term", DisplayOrder = 3 },
                    new FeeItemData { Id = "be-4", Category = "be", Component = "Lab & Workshop Fee", Amount = "4,000", Frequency = "Per Semester", Note = "For engineering lab access", DisplayOrder = 4 },
                    new FeeItemData { Id = "be-5", Category = "be", Component = "Library Fee", Amount = "1,500", Frequency = "Per Semester", Note = "Digital & physical library", DisplayOrder = 5 },
                    new FeeItemData { Id = "be-6", Category = "be", Component = "Activity Fee", Amount = "1,000", Frequency = "Per Semester", Note = "Sports, clubs, events", DisplayOrder = 6 },
                    new FeeItemData { Id = "be-sub", Category = "be", Component = "Semester Total", Amount = "54,500", Frequency = "Per Semester", Note = "Excl. one-time registration", IsSubtotal = true, DisplayOrder = 7 },
                    new FeeItemData { Id = "be-tot", Category = "be", Component = "Estimated Program Cost", Amount = "4,36,000", Frequency = "8 Semesters", Note = "4-year BE program total", IsTotal = true, DisplayOrder = 8 },

                    // BS
                    new FeeItemData { Id = "bs-1", Category = "bs", Component = "Tuition Fee", Amount = "38,000", Frequency = "Per Semester", Note = "BS Aviation Mgmt / Operations / Cyber Security", DisplayOrder = 1 },
                    new FeeItemData { Id = "bs-2", Category = "bs", Component = "Registration Fee", Amount = "5,000", Frequency = "One-Time", Note = "At time of admission", DisplayOrder = 2 },
                    new FeeItemData { Id = "bs-3", Category = "bs", Component = "Examination Fee", Amount = "2,500", Frequency = "Per Semester", Note = "Mid & final term", DisplayOrder = 3 },
                    new FeeItemData { Id = "bs-4", Category = "bs", Component = "Lab / IT Fee", Amount = "2,500", Frequency = "Per Semester", Note = "Computer & simulation lab", DisplayOrder = 4 },
                    new FeeItemData { Id = "bs-5", Category = "bs", Component = "Library Fee", Amount = "1,500", Frequency = "Per Semester", Note = "Digital & physical library", DisplayOrder = 5 },
                    new FeeItemData { Id = "bs-6", Category = "bs", Component = "Activity Fee", Amount = "1,000", Frequency = "Per Semester", Note = "Sports, clubs, events", DisplayOrder = 6 },
                    new FeeItemData { Id = "bs-sub", Category = "bs", Component = "Semester Total", Amount = "45,500", Frequency = "Per Semester", Note = "Excl. one-time registration", IsSubtotal = true, DisplayOrder = 7 },
                    new FeeItemData { Id = "bs-tot", Category = "bs", Component = "Estimated Program Cost", Amount = "3,64,000", Frequency = "8 Semesters", Note = "4-year BS program total", IsTotal = true, DisplayOrder = 8 },

                    // MBA
                    new FeeItemData { Id = "mba-1", Category = "mba", Component = "Tuition Fee", Amount = "55,000", Frequency = "Per Semester", Note = "MBA Aviation Management", DisplayOrder = 1 },
                    new FeeItemData { Id = "mba-2", Category = "mba", Component = "Registration Fee", Amount = "8,000", Frequency = "One-Time", Note = "At time of admission", DisplayOrder = 2 },
                    new FeeItemData { Id = "mba-3", Category = "mba", Component = "Examination Fee", Amount = "3,500", Frequency = "Per Semester", Note = "Mid & final term", DisplayOrder = 3 },
                    new FeeItemData { Id = "mba-4", Category = "mba", Component = "Library Fee", Amount = "2,000", Frequency = "Per Semester", Note = "Digital resources access", DisplayOrder = 4 },
                    new FeeItemData { Id = "mba-5", Category = "mba", Component = "Activity & Tech Fee", Amount = "1,500", Frequency = "Per Semester", Note = "LMS & digital tools", DisplayOrder = 5 },
                    new FeeItemData { Id = "mba-sub", Category = "mba", Component = "Semester Total", Amount = "62,000", Frequency = "Per Semester", Note = "Excl. one-time registration", IsSubtotal = true, DisplayOrder = 6 },
                    new FeeItemData { Id = "mba-tot", Category = "mba", Component = "Estimated Program Cost", Amount = "2,48,000", Frequency = "4 Semesters", Note = "2-year MBA program total", IsTotal = true, DisplayOrder = 7 },

                    // Short Courses
                    new FeeItemData { Id = "sc-1", Category = "short", Component = "3 Days Course", Amount = "8,000 – 12,000", Frequency = "Per Nominee", Note = "Course material, certificate", DisplayOrder = 1 },
                    new FeeItemData { Id = "sc-2", Category = "short", Component = "1 Week (5 Days)", Amount = "15,000 – 22,000", Frequency = "Per Nominee", Note = "Material, certificate, lunch", DisplayOrder = 2 },
                    new FeeItemData { Id = "sc-3", Category = "short", Component = "2 Weeks", Amount = "28,000 – 40,000", Frequency = "Per Nominee", Note = "Material, certificate, accommodation", DisplayOrder = 3 },
                    new FeeItemData { Id = "sc-4", Category = "short", Component = "3 Weeks", Amount = "40,000 – 55,000", Frequency = "Per Nominee", Note = "Full board, material, certificate", DisplayOrder = 4 },
                    new FeeItemData { Id = "sc-5", Category = "short", Component = "4–5 Weeks", Amount = "55,000 – 80,000", Frequency = "Per Nominee", Note = "Full board, simulator time", DisplayOrder = 5 },
                    new FeeItemData { Id = "sc-6", Category = "short", Component = "8 Weeks (ATC Assistant)", Amount = "1,10,000 – 1,40,000", Frequency = "Per Nominee", Note = "ATC simulator, full board, PCAA certificate", DisplayOrder = 6 }
                },
                FeeCards = new List<FeeCardData>
                {
                    new FeeCardData { Id = "fc-1", Category = "be", Title = "Per Semester", Amount = "PKR 54,500", Period = "Every 6 months", Note = "Includes tuition, exam, lab, library & activity fees", IconClass = "fas fa-university", BgColor = "#e8f0fe", TextColor = "#1a5fa6" },
                    new FeeCardData { Id = "fc-2", Category = "be", Title = "Annual Cost", Amount = "PKR 1,09,000", Period = "2 semesters/year", Note = "Based on standard 2-semester academic year", IconClass = "fas fa-calendar-check", BgColor = "#e6f4ea", TextColor = "#1e8e3e" },
                    new FeeCardData { Id = "fc-3", Category = "be", Title = "Registration (One-Time)", Amount = "PKR 5,000", Period = "Paid once at admission", Note = "Non-refundable after seat confirmation", IconClass = "fas fa-id-badge", BgColor = "#fff7e6", TextColor = "#f5a623" },
                    new FeeCardData { Id = "fc-4", Category = "be", Title = "Hostel (Optional)", Amount = "PKR 8,000", Period = "Per month", Note = "Subject to availability; apply separately", IconClass = "fas fa-graduation-cap", BgColor = "#fde8e8", TextColor = "#c0392b" }
                },
                Scholarships = new List<ScholarshipData>
                {
                    new ScholarshipData { Id = "sch-1", Title = "Merit Scholarship", Subtitle = "Top academic performers", Percentage = "50% – 100%", PercentageLabel = "Fee waiver on tuition", Criteria = new List<string> { "Minimum 85% marks in preceding qualifying exam (FSc/Intermediate)", "Top 3 positions in AU Entry Test within respective program", "Continuation requires CGPA of 3.5 or above each semester", "No disciplinary action on record" }, BadgeText = "Academic Excellence Award", BadgeBg = "#e8f0fe", BadgeColor = "#1a5fa6", HeaderGradient = "linear-gradient(135deg,#0b2c4d,#1a5fa6)" },
                    new ScholarshipData { Id = "sch-2", Title = "Need-Based Financial Aid", Subtitle = "Supporting deserving students", Percentage = "25% – 75%", PercentageLabel = "Tuition fee assistance", Criteria = new List<string> { "Household monthly income below PKR 50,000", "Attested income and utility bills required at application", "Minimum CGPA of 2.5 to maintain scholarship", "Priority given to orphans and students from under-developed areas" }, BadgeText = "Financial Assistance Grant", BadgeBg = "#e6f4ea", BadgeColor = "#1e7e34", HeaderGradient = "linear-gradient(135deg,#1e5c2e,#1e8e3e)" },
                    new ScholarshipData { Id = "sch-3", Title = "Hafiz-e-Quran Scholarship", Subtitle = "Government policy benefit", Percentage = "100%", PercentageLabel = "Full tuition waiver", Criteria = new List<string> { "Certified Hafiz-e-Quran with valid certificate from an authorized body", "Must meet minimum admission eligibility criteria", "Continuation requires CGPA of 2.0 or above" }, BadgeText = "Government Policy Scholarship", BadgeBg = "#fff7e6", BadgeColor = "#b8860b", HeaderGradient = "linear-gradient(135deg,#7b4f00,#c87800)" },
                    new ScholarshipData { Id = "sch-4", Title = "CAA Employee & Dependent Concession", Subtitle = "For PAA / CAA families", Percentage = "30% – 50%", PercentageLabel = "Fee concession on tuition", Criteria = new List<string> { "Children of serving or retired PAA/PCAA employees", "Employment verification letter from HR department required", "Minimum CGPA of 2.5 for continued eligibility" }, BadgeText = "Employee Benefit Scheme", BadgeBg = "#f3e8ff", BadgeColor = "#7c3aed", HeaderGradient = "linear-gradient(135deg,#4a1b6b,#7c3aed)" }
                },
                Banks = new List<BankData>
                {
                    new BankData { Id = "bank-1", Name = "Habib Bank Limited (HBL)", Details = "Over-the-counter & HBL Mobile app & HBL PayConnect", TagText = "Primary", TagBg = "#e6f4ea", TagColor = "#1e7e34", LogoText = "HBL", LogoBgHex = "#006b52", LogoUrl = "/images/banks/hbl.png" },
                    new BankData { Id = "bank-2", Name = "National Bank of Pakistan (NBP)", Details = "Over-the-counter & NBP Digital", TagText = "Accepted", TagBg = "#e8f0fe", TagColor = "#1a5fa6", LogoText = "NBP", LogoBgHex = "#006039", LogoUrl = "/images/banks/nbp.png" },
                    new BankData { Id = "bank-3", Name = "MCB Bank", Details = "Over-the-counter & MCB Internet Banking", TagText = "Accepted", TagBg = "#e8f0fe", TagColor = "#1a5fa6", LogoText = "MCB", LogoBgHex = "#ffffff", LogoUrl = "/images/banks/mcb.png" },
                    new BankData { Id = "bank-4", Name = "United Bank Limited (UBL)", Details = "Over-the-counter & UBL Digital App", TagText = "Accepted", TagBg = "#e8f0fe", TagColor = "#1a5fa6", LogoText = "UBL", LogoBgHex = "#005baa", LogoUrl = "/images/banks/ubl.png" },
                    new BankData { Id = "bank-5", Name = "Easypaisa", Details = "Mobile wallet payment via challan number & Easypaisa App", TagText = "Mobile", TagBg = "#e6f4ea", TagColor = "#1e7e34", LogoText = "EASY", LogoBgHex = "#00a859", LogoUrl = "/images/banks/easypaisa.png" },
                    new BankData { Id = "bank-6", Name = "JazzCash", Details = "Mobile wallet payment & retail shop agents", TagText = "Mobile", TagBg = "#fff7e6", TagColor = "#c87800", LogoText = "JAZZ", LogoBgHex = "#d81e05", LogoUrl = "/images/banks/jazzcash.png" }
                }
            };
        }
    }
}

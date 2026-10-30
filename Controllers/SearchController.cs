using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;

namespace NAU_Web.Controllers
{
    public class SearchController : Controller
    {
        private readonly NauDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SearchController(NauDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Search?q=...
        public async Task<IActionResult> Index(string? q)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                ViewBag.CurrentUserName = user?.FullName ?? User.Identity?.Name ?? "User";
            }

            var query = q?.Trim() ?? string.Empty;
            var vm = new SearchResultViewModel { Query = query };

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(vm);
            }

            var searchTerm = query.ToLower();

            // 1. Courses
            var courses = await _context.Courses
                .Include(c => c.School)
                .Where(c => c.Title.ToLower().Contains(searchTerm)
                         || (c.Description != null && c.Description.ToLower().Contains(searchTerm))
                         || (c.Topics != null && c.Topics.ToLower().Contains(searchTerm))
                         || (c.School != null && c.School.Name.ToLower().Contains(searchTerm)))
                .Take(10)
                .ToListAsync();

            foreach (var c in courses)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "Courses",
                    Title = c.Title,
                    Description = c.Description ?? c.Topics ?? "Course offered by " + (c.School?.Name ?? "NAU"),
                    Url = "/Courses/Public",
                    Badge = c.School?.Name ?? "Short Course",
                    IconClass = "fas fa-graduation-cap"
                });
            }

            // 2. Faculties
            var faculties = await _context.Faculties
                .Where(f => f.Name.ToLower().Contains(searchTerm)
                         || (f.Description != null && f.Description.ToLower().Contains(searchTerm)))
                .Take(5)
                .ToListAsync();

            foreach (var f in faculties)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "Faculties",
                    Title = f.Name,
                    Description = f.Description ?? "Academic faculty at NAU.",
                    Url = string.IsNullOrEmpty(f.Slug) ? "/Faculties/Public" : $"/Faculties/Detail/{f.Slug}",
                    Badge = "Faculty",
                    IconClass = string.IsNullOrEmpty(f.Icon) ? "fas fa-university" : f.Icon
                });
            }

            // 3. Facilities
            var facilities = await _context.Facilities
                .Where(f => f.Name.ToLower().Contains(searchTerm)
                         || (f.IntroText != null && f.IntroText.ToLower().Contains(searchTerm))
                         || (f.Category != null && f.Category.ToLower().Contains(searchTerm)))
                .Take(5)
                .ToListAsync();

            foreach (var f in facilities)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "Facilities",
                    Title = f.Name,
                    Description = f.IntroText ?? "Campus facility at NAU.",
                    Url = string.IsNullOrEmpty(f.Slug) ? "/Facilities/Public" : $"/Facilities/PublicDetails/{f.Slug}",
                    Badge = f.Category ?? "Facility",
                    IconClass = string.IsNullOrEmpty(f.CardIcon) ? "fas fa-building" : f.CardIcon
                });
            }

            // 4. Simulation Labs
            var labs = await _context.SimulationLabSections
                .Where(l => l.Title.ToLower().Contains(searchTerm)
                         || (l.Description != null && l.Description.ToLower().Contains(searchTerm))
                         || (l.Tag != null && l.Tag.ToLower().Contains(searchTerm)))
                .Take(5)
                .ToListAsync();

            foreach (var l in labs)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "Simulation Labs",
                    Title = l.Title,
                    Description = l.Description ?? "Advanced aviation simulator lab.",
                    Url = $"/SimulationLabs/Public#{l.SectionKey}",
                    Badge = l.Tag ?? "Lab",
                    IconClass = string.IsNullOrEmpty(l.IconClass) ? "fas fa-plane-departure" : l.IconClass
                });
            }

            // 5. News & Announcements
            var news = await _context.NewsPosts
                .Where(n => n.Title.ToLower().Contains(searchTerm)
                         || (n.Body != null && n.Body.ToLower().Contains(searchTerm))
                         || (n.Category != null && n.Category.ToLower().Contains(searchTerm)))
                .Take(5)
                .ToListAsync();

            foreach (var n in news)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "News",
                    Title = n.Title,
                    Description = !string.IsNullOrEmpty(n.Body) && n.Body.Length > 120 ? n.Body.Substring(0, 120) + "..." : (n.Body ?? ""),
                    Url = "/NewsPosts/Public",
                    Badge = n.Category ?? "News",
                    IconClass = string.IsNullOrEmpty(n.Icon) ? "fas fa-newspaper" : n.Icon
                });
            }

            // 6. Useful Links
            var links = await _context.Links
                .Where(l => l.Name.ToLower().Contains(searchTerm)
                         || (l.Description != null && l.Description.ToLower().Contains(searchTerm)))
                .Take(5)
                .ToListAsync();

            foreach (var l in links)
            {
                vm.Results.Add(new SearchItemViewModel
                {
                    Category = "Useful Links",
                    Title = l.Name,
                    Description = l.Description ?? "Useful portal/link",
                    Url = string.IsNullOrEmpty(l.Url) ? "/Links" : l.Url,
                    Badge = "Useful Link",
                    IconClass = string.IsNullOrEmpty(l.Icon) ? "fas fa-link" : l.Icon
                });
            }

            return View(vm);
        }

        // GET: /Search/Api?q=... (Live autocomplete JSON)
        [HttpGet]
        public async Task<IActionResult> Api(string? q)
        {
            var query = q?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            var searchTerm = query.ToLower();
            var results = new List<object>();

            // Courses
            var courses = await _context.Courses
                .Where(c => c.Title.ToLower().Contains(searchTerm))
                .Take(3)
                .Select(c => new { title = c.Title, category = "Course", url = "/Courses/Public", icon = "fas fa-graduation-cap" })
                .ToListAsync();
            results.AddRange(courses);

            // Faculties
            var faculties = await _context.Faculties
                .Where(f => f.Name.ToLower().Contains(searchTerm))
                .Take(2)
                .Select(f => new { title = f.Name, category = "Faculty", url = string.IsNullOrEmpty(f.Slug) ? "/Faculties/Public" : $"/Faculties/Detail/{f.Slug}", icon = "fas fa-university" })
                .ToListAsync();
            results.AddRange(faculties);

            // Facilities
            var facilities = await _context.Facilities
                .Where(f => f.Name.ToLower().Contains(searchTerm))
                .Take(2)
                .Select(f => new { title = f.Name, category = "Facility", url = string.IsNullOrEmpty(f.Slug) ? "/Facilities/Public" : $"/Facilities/PublicDetails/{f.Slug}", icon = "fas fa-building" })
                .ToListAsync();
            results.AddRange(facilities);

            // Labs
            var labs = await _context.SimulationLabSections
                .Where(l => l.Title.ToLower().Contains(searchTerm))
                .Take(2)
                .Select(l => new { title = l.Title, category = "Simulator Lab", url = $"/SimulationLabs/Public#{l.SectionKey}", icon = "fas fa-plane-departure" })
                .ToListAsync();
            results.AddRange(labs);

            // News
            var news = await _context.NewsPosts
                .Where(n => n.Title.ToLower().Contains(searchTerm))
                .Take(2)
                .Select(n => new { title = n.Title, category = "News", url = "/NewsPosts/Public", icon = "fas fa-newspaper" })
                .ToListAsync();
            results.AddRange(news);

            return Json(results.Take(8));
        }
    }
}

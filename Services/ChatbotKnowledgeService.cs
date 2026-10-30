using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class RawCandidateSnippet
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Keywords { get; set; } = string.Empty;
        public double Weight { get; set; } = 1.0;
    }

    public class ChatbotKnowledgeService : IChatbotKnowledgeService
    {
        private readonly NauDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static List<RawCandidateSnippet>? _cachedCandidatePool;
        private static DateTime _poolCacheTime = DateTime.MinValue;
        private static readonly SemaphoreSlim _poolLock = new(1, 1);

        public const string HomeSlug = "home";
        public const string FinancialSlug = "financial";
        public const string HistoryCaaSlug = "about-history-caa";
        public const string HistoryCatiSlug = "about-history-cati";
        public const string ManagementSlug = "about-management";
        public const string CatalogSlug = "course-catalog";

        public ChatbotKnowledgeService(NauDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<Dictionary<string, int>> GetKnowledgeStatsAsync()
        {
            var catalogCoursesCount = 0;
            try
            {
                var catalogContent = await LoadCourseCatalogContentAsync();
                if (catalogContent?.Schools != null)
                {
                    catalogCoursesCount = catalogContent.Schools.Sum(s => s.Courses?.Count ?? 0);
                }
            }
            catch { }

            return new Dictionary<string, int>
            {
                { "Pages", await _context.Pages.CountAsync() },
                { "Courses", await _context.Courses.CountAsync() },
                { "Atp2026Courses", catalogCoursesCount },
                { "Schools", await _context.Schools.CountAsync() },
                { "Programs", await _context.Programs.CountAsync() },
                { "Faculties", await _context.Faculties.CountAsync() },
                { "Facilities", await _context.Facilities.CountAsync() },
                { "SimulationLabs", await _context.SimulationLabSections.CountAsync() },
                { "NewsPosts", await _context.NewsPosts.CountAsync() },
                { "UsefulLinks", await _context.Links.CountAsync() },
                { "AcademicResults", await _context.AcademicCalendarResults.CountAsync() }
            };
        }

        public async Task<List<KnowledgeSnippet>> GetRelevantKnowledgeAsync(string query, int maxResults = 8)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<KnowledgeSnippet>();
            }

            var cleanQuery = query.Trim().ToLowerInvariant();
            var tokens = Tokenize(cleanQuery);
            var synonyms = ExpandSynonyms(tokens);

            var pool = await GetRawCandidatePoolAsync();

            var candidates = new List<KnowledgeSnippet>();
            foreach (var item in pool)
            {
                var score = ScoreItem(item.Keywords, item.Content, tokens, synonyms, item.Weight);
                if (score >= 2.0)
                {
                    candidates.Add(new KnowledgeSnippet
                    {
                        Category = item.Category,
                        Title = item.Title,
                        Content = item.Content,
                        Url = item.Url,
                        Icon = item.Icon,
                        Score = score
                    });
                }
            }

            return candidates
                .OrderByDescending(c => c.Score)
                .Take(maxResults)
                .ToList();
        }

        private async Task<List<RawCandidateSnippet>> GetRawCandidatePoolAsync()
        {
            if (_cachedCandidatePool != null && (DateTime.UtcNow - _poolCacheTime).TotalMinutes < 3)
            {
                return _cachedCandidatePool;
            }

            await _poolLock.WaitAsync();
            try
            {
                if (_cachedCandidatePool != null && (DateTime.UtcNow - _poolCacheTime).TotalMinutes < 3)
                {
                    return _cachedCandidatePool;
                }

                var pool = await BuildRawCandidatePoolFromDbAsync();
                _cachedCandidatePool = pool;
                _poolCacheTime = DateTime.UtcNow;
                return pool;
            }
            finally
            {
                _poolLock.Release();
            }
        }

        private async Task<List<RawCandidateSnippet>> BuildRawCandidatePoolFromDbAsync()
        {
            var candidates = new List<RawCandidateSnippet>();

            // 1. Contact & Admissions Helpdesk
            var contact = await _context.ContactSettings.FirstOrDefaultAsync();
            var contactAddress = contact?.Address ?? "Airport Road, Civil Aviation Training Institute (CATI), Hyderabad, Sindh, Pakistan";
            var contactPhone = contact?.Phone ?? "+92 22 9260310-19";
            var contactEmail = contact?.Email ?? "AdID.Academics@paa.gov.pk";
            var contactHours = contact?.OfficeHours ?? "Monday to Friday, 08:00 AM - 04:00 PM";

            var contactContent = $"Official Address: {contactAddress}\n" +
                                 $"Admissions & General Helplines: {contactPhone}\n" +
                                 $"Admissions & Inquiry Email: {contactEmail}\n" +
                                 $"Office Operating Hours: {contactHours}\n" +
                                 $"Online Admissions Registration: /Account/Register (Sign up and apply online)";

            candidates.Add(new RawCandidateSnippet
            {
                Category = "Contact & Admissions Helpdesk",
                Title = contact?.Heading ?? "Contact National Aviation University (NAU)",
                Content = contactContent,
                Url = "/Contact",
                Icon = "fas fa-headset",
                Keywords = "contact phone email address location helpline office timing rabta dakhla number call inquiry admission apply",
                Weight = 3.0
            });

            // 2. Financial, Tuition Fee Structure & Scholarships
            var finPage = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == FinancialSlug);
            var finContent = GetStructuredFinancialDetails(finPage?.ContentHtml);
            candidates.Add(new RawCandidateSnippet
            {
                Category = "Fee Structure, Scholarships & Payment Methods",
                Title = "NAU Tuition Fees, Scholarships & Payment Details",
                Content = finContent,
                Url = "/Financial",
                Icon = "fas fa-coins",
                Keywords = "fee fees tuition cost scholarship financial aid discount payment bank challan installment charges paisa kharcha admission fee hostel fee",
                Weight = 3.5
            });

            // 3. Academic Programs & Degrees
            var programs = await _context.Programs
                .Include(p => p.Faculty)
                .Include(p => p.ProgramHighlights)
                .Include(p => p.ProgramCareers)
                .AsNoTracking()
                .ToListAsync();

            foreach (var prog in programs)
            {
                var highlights = prog.ProgramHighlights != null && prog.ProgramHighlights.Any()
                    ? string.Join("; ", prog.ProgramHighlights.Select(h => h.HighlightText))
                    : "";
                var careers = prog.ProgramCareers != null && prog.ProgramCareers.Any()
                    ? string.Join(", ", prog.ProgramCareers.Select(k => k.CareerTitle))
                    : "";

                var desc = $"Degree: {prog.Name} ({prog.Code}). Faculty: {prog.Faculty?.Name ?? "NAU Faculty"}. " +
                           $"Department: {prog.Department ?? ""}. " +
                           $"Undergrad Duration: {prog.UndergradYears ?? "4 Years (8 Semesters)"}. ADP: {prog.Adpyears ?? "2 Years"}. Graduate: {prog.GradYears ?? "2 Years"}. " +
                           $"Overview: {prog.Overview ?? ""}. " +
                           (string.IsNullOrEmpty(highlights) ? "" : $"Key Highlights: {highlights}. ") +
                           (string.IsNullOrEmpty(careers) ? "" : $"Career Opportunities: {careers}. ") +
                           "Eligibility: Minimum 50%-60% in FSc Pre-Engineering / ICS / A-Levels or equivalent. Apply online at /Account/Register.";

                var url = !string.IsNullOrEmpty(prog.Faculty?.Slug) ? $"/Faculties/Detail/{prog.Faculty.Slug}" : "/CourseCatalog";

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Degree Programs & Admissions",
                    Title = prog.Name,
                    Content = desc,
                    Url = url,
                    Icon = string.IsNullOrEmpty(prog.Icon) ? "fas fa-award" : prog.Icon,
                    Keywords = prog.Name + " " + prog.Code + " degree bachelors bs be masters admission eligibility criteria requirements parhai",
                    Weight = 2.5
                });
            }

            // 4. Faculties & Academic Departments
            var faculties = await _context.Faculties.AsNoTracking().ToListAsync();
            foreach (var f in faculties)
            {
                var desc = $"Faculty of {f.Name}. {f.Description ?? ""}. Departments offer world-class aviation curricula in partnership with Pakistan Civil Aviation Authority (PCAA).";
                var url = string.IsNullOrEmpty(f.Slug) ? "/Faculties/Public" : $"/Faculties/Detail/{f.Slug}";

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Academic Faculties",
                    Title = f.Name,
                    Content = desc,
                    Url = url,
                    Icon = string.IsNullOrEmpty(f.Icon) ? "fas fa-university" : f.Icon,
                    Keywords = f.Name + " faculty department professors academics degrees shoba",
                    Weight = 2.0
                });
            }

            // 5. ATP 2026 Course Catalog (300+ Courses across Air Navigation, Regulatory, Airport, Management)
            var catalogContent = await LoadCourseCatalogContentAsync();
            if (catalogContent?.Schools != null && catalogContent.Schools.Any())
            {
                foreach (var sch in catalogContent.Schools)
                {
                    var schCourses = sch.Courses ?? new List<CourseCatalogItem>();
                    var sampleCourses = schCourses.Take(15).Select(c => $"{c.Title} (Duration: {c.Duration}, Dates: {c.StartDate} to {c.EndDate})");
                    var courseListStr = string.Join("; ", sampleCourses);

                    var desc = $"School of {sch.Name} (ATP 2026 Academic Training Plan). Total Courses: {schCourses.Count}. " +
                               $"Key Certified Courses: {courseListStr}. All courses provide official PCAA recognized certification.";

                    candidates.Add(new RawCandidateSnippet
                    {
                        Category = "ATP 2026 Course Catalog",
                        Title = $"ATP 2026 - {sch.Name}",
                        Content = desc,
                        Url = "/CourseCatalog",
                        Icon = "fas fa-book-open",
                        Keywords = sch.Name + " atp 2026 catalog short course training aviation cert schedule atc radar pilot air navigation",
                        Weight = 2.4
                    });
                }
            }

            // 6. Database Courses & Schools
            var dbCourses = await _context.Courses
                .Include(c => c.School)
                .AsNoTracking()
                .ToListAsync();

            foreach (var c in dbCourses)
            {
                var schoolName = c.School?.Name ?? "Aviation Academy";
                var desc = $"{c.Title}. School: {schoolName}. " +
                           $"Duration: {c.Duration ?? "Variable"}. " +
                           $"Overview: {c.Description ?? ""}. Topics: {c.Topics ?? ""}. Highlights: {c.Highlights ?? ""}. Certificate: {c.Certificate ?? "PCAA / NAU Certificate"}";

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Short Courses & Training",
                    Title = c.Title,
                    Content = desc,
                    Url = "/Courses/Public",
                    Icon = "fas fa-graduation-cap",
                    Keywords = c.Title + " " + schoolName + " short course training fee syllabus certificate",
                    Weight = 2.2
                });
            }

            // 7. Facilities (Hostel, Library, Sports, Medical, Mosque, Smart Campus, Mess, Cafeteria)
            var facilities = await _context.Facilities
                .Include(f => f.FacilityHours)
                .Include(f => f.FacilityServices)
                .Include(f => f.FacilityStats)
                .AsNoTracking()
                .ToListAsync();

            foreach (var fac in facilities)
            {
                var hours = fac.FacilityHours != null && fac.FacilityHours.Any()
                    ? string.Join("; ", fac.FacilityHours.Select(h => $"{h.DayOrLabel}: {h.TimeValue}"))
                    : (fac.ShortHours ?? "Daily campus operating hours");

                var services = fac.FacilityServices != null && fac.FacilityServices.Any()
                    ? string.Join("; ", fac.FacilityServices.Select(s => s.Title + (string.IsNullOrEmpty(s.Description) ? "" : ": " + s.Description)))
                    : "";

                var stats = fac.FacilityStats != null && fac.FacilityStats.Any()
                    ? string.Join("; ", fac.FacilityStats.Select(st => $"{st.StatNumber} {st.StatLabel}"))
                    : "";

                var desc = $"Facility: {fac.Name} (Category: {fac.Category ?? "Campus Facility"}). Overview: {fac.IntroText ?? ""}. " +
                           $"Operating Hours: {hours}. " +
                           (string.IsNullOrEmpty(services) ? "" : $"Services: {services}. ") +
                           (string.IsNullOrEmpty(stats) ? "" : $"Key Highlights: {stats}.");

                var url = string.IsNullOrEmpty(fac.Slug) ? "/Facilities/Public" : $"/Facilities/PublicDetails/{fac.Slug}";

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Campus Facilities & Student Life",
                    Title = fac.Name,
                    Content = desc,
                    Url = url,
                    Icon = string.IsNullOrEmpty(fac.CardIcon) ? "fas fa-building" : fac.CardIcon,
                    Keywords = fac.Name + " " + (fac.Category ?? "") + " hostel accommodation room rehaish residence library books sports gym cricket mess food khana medical clinic doctor masjid mosque prayer timing",
                    Weight = 2.8
                });
            }

            // 8. Simulation & Aviation Labs
            var simLabs = await _context.SimulationLabSections
                .Include(s => s.SimulationLabFeatures)
                .Include(s => s.SimulationLabInfoCard)
                .ThenInclude(c => c != null ? c.SimulationLabInfoItems : null)
                .AsNoTracking()
                .ToListAsync();

            foreach (var lab in simLabs)
            {
                var features = lab.SimulationLabFeatures != null && lab.SimulationLabFeatures.Any()
                    ? string.Join("; ", lab.SimulationLabFeatures.Select(f => f.Label))
                    : "";
                var items = lab.SimulationLabInfoCard?.SimulationLabInfoItems != null && lab.SimulationLabInfoCard.SimulationLabInfoItems.Any()
                    ? string.Join("; ", lab.SimulationLabInfoCard.SimulationLabInfoItems.Select(i => i.Text))
                    : "";

                var desc = $"Simulation Lab: {lab.Title} ({lab.Tag ?? "Flight Simulation"}). Description: {lab.Description ?? ""}. " +
                           (string.IsNullOrEmpty(features) ? "" : $"Features: {features}. ") +
                           (string.IsNullOrEmpty(items) ? "" : $"Capabilities: {items}.");

                var url = $"/SimulationLabs/Public#{lab.SectionKey}";

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Simulation Labs & Aviation Tech",
                    Title = lab.Title,
                    Content = desc,
                    Url = url,
                    Icon = string.IsNullOrEmpty(lab.IconClass) ? "fas fa-plane-departure" : lab.IconClass,
                    Keywords = lab.Title + " " + (lab.Tag ?? "") + " simulator flight simulation boeing b737 airbus a320 radar atc tower avionics cockpit pilot tayara",
                    Weight = 2.6
                });
            }

            // 9. News, Events & Admissions Announcements
            var news = await _context.NewsPosts
                .OrderByDescending(n => n.PublishDate)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in news)
            {
                var desc = $"News Announcement ({item.Category ?? "Notice"}): {item.Title}. Date: {item.PublishDate:dd MMM yyyy}. Details: {item.Body ?? ""}";
                candidates.Add(new RawCandidateSnippet
                {
                    Category = "News & Announcements",
                    Title = item.Title,
                    Content = desc,
                    Url = "/NewsPosts/Public",
                    Icon = string.IsNullOrEmpty(item.Icon) ? "fas fa-newspaper" : item.Icon,
                    Keywords = item.Title + " " + (item.Category ?? "") + " news announcement event notice merit list intake",
                    Weight = 1.8
                });
            }

            // 9b. Upcoming University Events, Seminars & Workshops
            try
            {
                var todayDate = DateOnly.FromDateTime(DateTime.Now);
                var upcomingEvents = await _context.Events
                    .Where(e => e.IsActive && e.EventDate >= todayDate)
                    .OrderBy(e => e.EventDate)
                    .Take(10)
                    .AsNoTracking()
                    .ToListAsync();

                if (!upcomingEvents.Any())
                {
                    upcomingEvents = await _context.Events
                        .Where(e => e.IsActive)
                        .OrderBy(e => e.EventDate)
                        .Take(10)
                        .AsNoTracking()
                        .ToListAsync();
                }

                if (upcomingEvents.Any())
                {
                    var eventDetailsList = upcomingEvents.Select(e =>
                        $"• **{e.Title}**\n  - **Date & Time:** {e.EventDate:dd MMMM yyyy} ({e.StartTime:hh\\:mm} to {e.EndTime:hh\\:mm})\n  - **Location:** {e.Location ?? "NAU Campus"}");

                    var eventsSummary = "**Upcoming Official NAU Events & Seminars 2026:**\n\n" + string.Join("\n\n", eventDetailsList);

                    candidates.Add(new RawCandidateSnippet
                    {
                        Category = "Upcoming Events & Seminars",
                        Title = "Upcoming NAU Events, Seminars & Workshops 2026",
                        Content = eventsSummary,
                        Url = "/NewsPosts/Public",
                        Icon = "fas fa-calendar-alt",
                        Keywords = "event events upcoming seminar workshop gala convocation schedule date time location venue program ceremony kab hoga kahan hoga held when where",
                        Weight = 6.0
                    });

                    foreach (var ev in upcomingEvents)
                    {
                        var singleEventDesc = $"Upcoming Event: {ev.Title}. Date: {ev.EventDate:dd MMMM yyyy}. Time: {ev.StartTime:hh\\:mm} to {ev.EndTime:hh\\:mm}. Location: {ev.Location ?? "NAU Campus"}.";
                        candidates.Add(new RawCandidateSnippet
                        {
                            Category = "Upcoming Events",
                            Title = ev.Title,
                            Content = singleEventDesc,
                            Url = "/NewsPosts/Public",
                            Icon = "fas fa-calendar-check",
                            Keywords = ev.Title + " event upcoming seminar workshop date time location held when where",
                            Weight = 4.5
                        });
                    }
                }
            }
            catch { }

            // 10. CMS Pages (About History CAA, CATI, Management & Governance)
            var allPages = await _context.Pages
                .Where(p => p.IsPublished && p.Slug != FinancialSlug && p.Slug != CatalogSlug)
                .AsNoTracking()
                .ToListAsync();

            foreach (var page in allPages)
            {
                if (string.IsNullOrWhiteSpace(page.ContentHtml)) continue;

                var textContent = StripHtml(page.ContentHtml);
                var pageUrl = page.Slug switch
                {
                    HomeSlug => "/",
                    HistoryCaaSlug => "/about.html#caa",
                    HistoryCatiSlug => "/about.html#cati",
                    ManagementSlug => "/about.html#management",
                    _ => page.Slug.StartsWith("facility-")
                        ? $"/Facilities/PublicDetails/{page.Slug.Replace("facility-", "").Replace("-content", "")}"
                        : $"/Pages/Details/{page.PageId}"
                };

                candidates.Add(new RawCandidateSnippet
                {
                    Category = "University History & Background",
                    Title = page.Title ?? page.Slug,
                    Content = Truncate(textContent, 900),
                    Url = pageUrl,
                    Icon = "fas fa-university",
                    Keywords = page.Title + " " + page.Slug,
                    Weight = 1.2
                });
            }

            // 11. Useful Links & Student Portals
            var links = await _context.Links.AsNoTracking().ToListAsync();
            if (links.Any())
            {
                var linksSummary = string.Join("; ", links.Select(l => $"{l.Name}: {l.Url} ({l.Description ?? ""})"));
                candidates.Add(new RawCandidateSnippet
                {
                    Category = "Portals & Quick Links",
                    Title = "Student, Faculty & Learning Portals",
                    Content = linksSummary + " | Apply Online: /Account/Register | Student Login: /Account/Login",
                    Url = "/Links",
                    Icon = "fas fa-link",
                    Keywords = "portal login register student teacher links downloads library moodle lms account",
                    Weight = 1.8
                });
            }

            // 12. Admissions Guidance & How to Apply
            candidates.Add(new RawCandidateSnippet
            {
                Category = "Admissions & Application Procedure",
                Title = "How to Apply for Admissions at NAU",
                Content = "Step 1: Visit the NAU Admissions Registration Portal at /Account/Register.\n" +
                          "Step 2: Fill in personal, educational, and program choices.\n" +
                          "Step 3: Upload required documents (Matric/FSc Marksheets, CNIC/B-Form, Photographs).\n" +
                          "Step 4: Generate and pay the admission processing challan at any designated bank (NBP, HBL, MCB, UBL).\n" +
                          "Step 5: Appear in the NAU Entry Test / Interview.\n" +
                          "Admissions Helpline: +92 22 9260310-19 | Email: AdID.Academics@paa.gov.pk.",
                Url = "/Account/Register",
                Icon = "fas fa-user-plus",
                Keywords = "apply admission admissions registration dakhla form procedure steps requirements eligibility criteria",
                Weight = 3.2
            });

            // 13. Required Documents (General, Faculty-Wise & School-Wise)
            candidates.Add(new RawCandidateSnippet
            {
                Category = "Admissions & Document Requirements",
                Title = "Required Documents for Admission (General, Faculty & School Side)",
                Content = "1. General Documents (All Applicants): CNIC/B-Form, Father's CNIC, 4 Passport-size Photographs (white background), Paid Fee Challan, Domicile/PRC, Character Certificate.\n" +
                          "2. Faculty of Engineering (BE ANS / Airport Engg): FSc Pre-Engineering / A-Levels DMC (min. 50%), IBCC Equivalence, AU/NTS Entry Test, Medical Fitness & Color Vision Certificate.\n" +
                          "3. Faculty of IT & Cyber Security (BS Aviation Cyber Security): FSc Pre-Engg / ICS / A-Levels DMC (min. 50% with Math/CS), Matric Science DMC, Entry Test Scorecard.\n" +
                          "4. Faculty of Aviation Operations (BS Aviation Operations/Mgmt): Intermediate (FA/FSc/ICS, min. 45%), Entry Test Result, Medical Fitness Clearance.\n" +
                          "5. Faculty of Management Sciences (MBA Aviation): 16-Year HEC-Attested Bachelor's Degree & Transcripts, GAT General Score (min. 50%), 2-yr Experience Letter & NOC (for Executive track).\n" +
                          "6. School of ANS / ATC: Official PCAA/Agency Nomination Letter, Class 1/3 CAA Medical Clearance, ICAO ELP Rating.\n" +
                          "7. School of Airport Services: Official Nomination, ARFF Physical Endurance & Medical Fitness Certificate, Security Clearance.\n" +
                          "8. School of Regulatory Services: Official Nomination, Aviation Personnel License (ATSEP/PCAA/Flight Crew), Passport & Visa (foreign trainees).\n" +
                          "9. Special Quotas: CAA/PAA Employee Service Certificate & Endorsement, Hafiz-e-Quran Sanad (+20 marks), Disability Certificate.",
                Url = "/Account/Register",
                Icon = "fas fa-file-upload",
                Keywords = "required documents documents needed kagazat faculty school engineering it ans airport regulatory management certificate dmc cnic fsc matric bform fee challan",
                Weight = 3.5
            });

            return candidates;
        }

        public async Task<string> GetMasterKnowledgeSummaryAsync()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== NATIONAL AVIATION UNIVERSITY (NAU) OFFICIAL FACTBOOK ===");
            sb.AppendLine("Institution: National Aviation University (NAU), established at Civil Aviation Training Institute (CATI) Hyderabad under Pakistan Civil Aviation Authority (PCAA).");
            sb.AppendLine("Accreditations & Recognition: Higher Education Commission (HEC) of Pakistan, Pakistan Engineering Council (PEC), PCAA, and ICAO compliant curricula.");
            sb.AppendLine("Location & Address: Airport Road, Civil Aviation Training Institute (CATI), Hyderabad, Sindh, Pakistan.");
            sb.AppendLine("Admissions Helpline: +92 22 9260310-19 | Email: AdID.Academics@paa.gov.pk | Office Hours: Mon-Fri, 08:00 AM - 04:00 PM.");
            sb.AppendLine("--- INSTITUTIONAL ACADEMIC STRUCTURE ---");
            sb.AppendLine("NAU has two main pillars: 4 Academic Faculties (for Degree Programs) and 4 Specialized Schools (for ATP 2026 Professional Aviation Short Courses).\n");

            sb.AppendLine("--- 4 ACADEMIC FACULTIES (DEGREE PROGRAMS) ---");
            sb.AppendLine("1. Faculty of Engineering: BE Air Navigation Services (ANS) Engineering & Airport Engineering (4 Years / 8 Sem)");
            sb.AppendLine("2. Faculty of IT & Cyber Security: BS Computer Science & AI, BS Cyber Security & Data Science (4 Years / 8 Sem)");
            sb.AppendLine("3. Faculty of Aviation Operations: BS Aviation Management, BS Aircraft Maintenance Technology (4 Years / 8 Sem)");
            sb.AppendLine("4. Faculty of Management Sciences: Aviation Management, MBA Aviation Management (2 Years / 4 Sem)");
            sb.AppendLine("Eligibility: Minimum 50%-60% marks in FSc Pre-Engineering / ICS / A-Levels (or Graduation for MBA). Apply online at /Account/Register.\n");

            sb.AppendLine("--- 4 SPECIALIZED AVIATION TRAINING SCHOOLS (ATP 2026 SHORT COURSES) ---");
            sb.AppendLine("1. School of Air Navigation Services (ANS): Basic ATC (40 wks), ATC Refresher, Radar & CNS Training");
            sb.AppendLine("2. School of Regulatory Services: Flight Safety, Aerodrome Standards, Airworthiness Inspection");
            sb.AppendLine("3. School of Airport Services: Aviation Security (AVSEC), Aerodrome Firefighting (AFF)");
            sb.AppendLine("4. School of Aviation Management: Safety Management Systems (SMS), Aviation Leadership, CPD Workshops\n");

            sb.AppendLine("--- 6 DEGREE PROGRAMS SUMMARY ---");
            sb.AppendLine("- BE Air Navigation Services (ANS) Engineering & Airport Engineering (4 Years / 8 Semesters)");
            sb.AppendLine("- BS Aviation Management (4 Years / 8 Semesters)");
            sb.AppendLine("- BS Computer Science & Artificial Intelligence (4 Years / 8 Semesters)");
            sb.AppendLine("- BS Cyber Security & Data Science (4 Years / 8 Semesters)");
            sb.AppendLine("- BS Aircraft Maintenance Technology (4 Years / 8 Semesters)");
            sb.AppendLine("- MBA Aviation Management (2 Years / 4 Semesters)\n");

            sb.AppendLine("--- TUITION FEES & FINANCIAL STRUCTURE (2026) ---");
            sb.AppendLine("- BE Programs: PKR 45,000 tuition/sem + PKR 9,500 charges = PKR 54,500/semester (8 Semesters Total: PKR 4,36,000)");
            sb.AppendLine("- BS Programs: PKR 38,000 tuition/sem + PKR 7,500 charges = PKR 45,500/semester (8 Semesters Total: PKR 3,64,000)");
            sb.AppendLine("- MBA Program: PKR 55,000 tuition/sem + PKR 7,000 charges = PKR 62,000/semester (4 Semesters Total: PKR 2,48,000)");
            sb.AppendLine("- One-Time Admission Registration Fee: PKR 5,000 (BE/BS) / PKR 8,000 (MBA)");
            sb.AppendLine("- Hostel Accommodation: PKR 8,000 per month (optional, subject to availability)");
            sb.AppendLine("- Payment Methods: Bank challan at HBL, NBP, MCB, or UBL branches, or through the online Student Portal.\n");

            sb.AppendLine("--- SCHOLARSHIPS & FINANCIAL AID ---");
            sb.AppendLine("- Merit Scholarship: 50% to 100% tuition waiver (Min 85% in FSc/Inter or Top 3 in AU Entry test; maintain CGPA 3.5+)");
            sb.AppendLine("- Need-Based Financial Aid: 25% to 75% tuition assistance (Household income < PKR 50,000/mo; maintain CGPA 2.5+)");
            sb.AppendLine("- Hafiz-e-Quran Scholarship: 100% full tuition waiver (Certified Hafiz-e-Quran; maintain CGPA 2.0+)");
            sb.AppendLine("- PAA / CAA Employee & Dependent Concession: 30% to 50% tuition concession (Children of serving/retired staff)\n");

            sb.AppendLine("--- CAMPUS FACILITIES & SIMULATORS ---");
            sb.AppendLine("- Hostels: Separate secure on-campus hostels for boys and girls with furnished rooms, 24/7 power backup, Wi-Fi, and mess.");
            sb.AppendLine("- Central Library: Over 15,000 technical aviation and engineering volumes, e-library portal, research databases.");
            sb.AppendLine("- Sports Complex: Cricket stadium, tennis courts, modern gym/fitness center, table tennis, badminton, athletic grounds.");
            sb.AppendLine("- Medical Center: 24/7 emergency medical assistance, doctor and nurses on duty, on-campus ambulance service.");
            sb.AppendLine("- Central Mosque: Air-conditioned prayer hall accommodating students, faculty, and campus residents.");
            sb.AppendLine("- Simulation Labs: Boeing B737-800 Flight Simulator, Airbus A320 FTD, 360 Aerodrome ATC Control Tower Simulator, Radar Systems Lab, Avionics Lab.\n");

            sb.AppendLine("--- 4 SPECIALIZED AVIATION SCHOOLS ---");
            sb.AppendLine("- School of Air Navigation Services (ANS): Basic & Advanced ATC, CNS, and Radar Training");
            sb.AppendLine("- School of Regulatory Services: Flight Safety, Aerodrome Standards, Airworthiness Inspection");
            sb.AppendLine("- School of Airport Services: Aviation Security (AVSEC), Aerodrome Firefighting (AFF)");
            sb.AppendLine("- School of Aviation Management: Safety Management Systems (SMS), Aviation Leadership, CPD\n");

            sb.AppendLine("--- ATP 2026 SHORT COURSES & PROFESSIONAL TRAINING ---");
            sb.AppendLine("Over 300 certified aviation short courses across the 4 schools mentioned above.");
            sb.AppendLine("Explore complete catalog: [Course Catalog](/CourseCatalog)");

            return sb.ToString();
        }

        private async Task<CourseCatalogPageContent?> LoadCourseCatalogContentAsync()
        {
            try
            {
                var page = await _context.Pages.FirstOrDefaultAsync(p => p.Slug == CatalogSlug);
                if (page != null && !string.IsNullOrWhiteSpace(page.ContentHtml))
                {
                    return JsonSerializer.Deserialize<CourseCatalogPageContent>(page.ContentHtml);
                }

                var seedPath = Path.Combine(_env.ContentRootPath, "SeedData", "course-catalog-content.json");
                if (System.IO.File.Exists(seedPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(seedPath);
                    return JsonSerializer.Deserialize<CourseCatalogPageContent>(json);
                }
            }
            catch { }
            return null;
        }

        private static string GetStructuredFinancialDetails(string? htmlContent)
        {
            var defaultFinancial =
                "Tuition Fee Structure (2026 Academic Year):\n" +
                "- BE Programs (ANS / Airport Engineering): PKR 54,500/semester (Tuition PKR 45,000 + Exam/Lab/Library/Activity PKR 9,500). Estimated 4-year total: PKR 4,36,000.\n" +
                "- BS Programs (Aviation Management, CS, Cyber Security): PKR 45,500/semester (Tuition PKR 38,000 + Exam/Lab/Library/Activity PKR 7,500). Estimated 4-year total: PKR 3,64,000.\n" +
                "- MBA Aviation Management: PKR 62,000/semester (Tuition PKR 55,000 + Exam/Library/Activity PKR 7,000). Estimated 2-year total: PKR 2,48,000.\n" +
                "- One-Time Admission Registration Fee: PKR 5,000 (BE/BS) / PKR 8,000 (MBA).\n" +
                "- Hostel Fee (Optional): PKR 8,000 per month (includes room, utility, security; mess billed separately).\n" +
                "- Short Course Fees: 3 Days (PKR 8,000–12,000), 1 Week (PKR 15,000–22,000), 2 Weeks (PKR 28,000–40,000), 3 Weeks (PKR 40,000–55,000), 4–5 Weeks (PKR 55,000–80,000), 8 Weeks ATC Assistant (PKR 1,10,000–1,40,000).\n\n" +
                "Scholarships & Financial Aid:\n" +
                "- Merit Scholarship: 50%–100% tuition waiver for students with 85%+ in FSc or top 3 entry test rank (CGPA 3.5+ required).\n" +
                "- Need-Based Financial Aid: 25%–75% tuition assistance for household income below PKR 50,000/month (CGPA 2.5+ required).\n" +
                "- Hafiz-e-Quran Scholarship: 100% full tuition waiver for verified Hafiz-e-Quran candidates.\n" +
                "- CAA / PAA Employee & Dependent Concession: 30%–50% tuition concession for children of serving or retired staff.\n\n" +
                "Accepted Payment Methods:\n" +
                "- Bank Challan at any HBL, National Bank of Pakistan (NBP), MCB Bank, or UBL branch.\n" +
                "- Online Student Portal challan download at /Account/Login.";

            if (string.IsNullOrWhiteSpace(htmlContent)) return defaultFinancial;

            try
            {
                var finData = JsonSerializer.Deserialize<FinancialPageContent>(htmlContent);
                if (finData != null && finData.FeeCards != null && finData.FeeCards.Any())
                {
                    var feeCards = string.Join("; ", finData.FeeCards.Select(c => $"{c.Title}: {c.Amount} ({c.Period})"));
                    var scholarships = finData.Scholarships != null ? string.Join("; ", finData.Scholarships.Select(s => $"{s.Title} ({s.Percentage}): {s.Subtitle}")) : "";
                    var banks = finData.Banks != null ? string.Join("; ", finData.Banks.Select(b => $"{b.Name}: {b.Details}")) : "";

                    return $"Tuition & Fees: {feeCards}.\nScholarships: {scholarships}.\nAccepted Banks: {banks}.\nNotice: {finData.FeeNoticeText ?? ""}";
                }
            }
            catch { }

            return defaultFinancial;
        }

        private static double ScoreItem(string titleAndKeywords, string content, List<string> queryTokens, List<string> synonyms, double weightMultiplier)
        {
            if (queryTokens.Count == 0) return 0;

            double score = 0;
            var lowerTitle = titleAndKeywords.ToLowerInvariant();
            var lowerContent = content.ToLowerInvariant();

            foreach (var token in queryTokens)
            {
                if (string.IsNullOrWhiteSpace(token) || token.Length < 2) continue;

                if (lowerTitle.Contains(token))
                {
                    score += 4.0 * weightMultiplier;
                }
                else if (lowerContent.Contains(token))
                {
                    score += 1.2 * weightMultiplier;
                }
            }

            foreach (var syn in synonyms)
            {
                if (lowerTitle.Contains(syn))
                {
                    score += 2.5 * weightMultiplier;
                }
                else if (lowerContent.Contains(syn))
                {
                    score += 0.9 * weightMultiplier;
                }
            }

            return score;
        }

        private static List<string> Tokenize(string query)
        {
            var stopWords = new HashSet<string>
            {
                "the", "is", "at", "which", "on", "a", "an", "and", "or", "in", "for", "to", "of", "with",
                "what", "when", "where", "how", "who", "tell", "me", "about", "can", "you", "please",
                "kia", "hai", "kya", "ko", "se", "ka", "ki", "ke", "karen", "hota", "hoti", "hain",
                "batao", "bataen", "bataiye", "mujhe", "he", "hn", "aur", "ya", "par", "pe", "karo", "do"
            };

            var words = Regex.Split(query, @"[\s\p{P}]+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 2 && !stopWords.Contains(w.ToLowerInvariant()))
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .ToList();

            return words;
        }

        private static List<string> ExpandSynonyms(List<string> tokens)
        {
            var result = new List<string>();
            var synMap = new Dictionary<string, string[]>
            {
                { "fee", new[] { "fees", "tuition", "cost", "charges", "paisa", "scholarship", "financial", "rate", "dues", "challan", "kharcha", "pesay" } },
                { "fees", new[] { "fee", "tuition", "cost", "charges", "paisa", "scholarship", "financial", "rate", "dues", "challan", "kharcha", "pesay" } },
                { "cost", new[] { "fee", "fees", "tuition", "charges", "price", "paisa", "kharcha" } },
                { "admission", new[] { "admissions", "apply", "dakhla", "enroll", "eligibility", "intake", "seat", "seats", "criteria", "registration", "form" } },
                { "admissions", new[] { "admission", "apply", "dakhla", "enroll", "eligibility", "intake", "seat", "seats", "criteria", "registration", "form" } },
                { "dakhla", new[] { "admission", "admissions", "apply", "form", "eligibility", "intake", "registration" } },
                { "apply", new[] { "admission", "enroll", "dakhla", "form", "application", "register" } },
                { "hostel", new[] { "hostels", "accommodation", "room", "rooms", "rehaish", "residence", "mess", "stay", "living", "facility", "facilities" } },
                { "facility", new[] { "facilities", "hostel", "library", "sports", "mess", "masjid", "mosque", "gym", "medical", "campus" } },
                { "facilities", new[] { "facility", "hostel", "library", "sports", "mess", "masjid", "mosque", "gym", "medical", "campus" } },
                { "mosque", new[] { "masjid", "namaz", "prayer", "worship", "azan", "jummah", "facility" } },
                { "masjid", new[] { "mosque", "namaz", "prayer", "worship", "azan", "jummah", "facility" } },
                { "contact", new[] { "phone", "email", "number", "address", "rabta", "location", "timing", "call", "helpdesk", "office", "pata" } },
                { "rabta", new[] { "contact", "phone", "email", "number", "address", "helpline", "call" } },
                { "course", new[] { "courses", "short", "training", "atp", "program", "programs", "degree", "curriculum", "syllabus", "parhai" } },
                { "courses", new[] { "course", "short", "training", "atp", "program", "programs", "degree", "curriculum", "syllabus", "parhai" } },
                { "catalog", new[] { "course", "courses", "atp", "2026", "short courses", "training plan" } },
                { "sim", new[] { "simulator", "simulation", "flight", "pilot", "atc", "radar", "cockpit", "b737", "a320", "tayara" } },
                { "simulator", new[] { "simulation", "flight", "pilot", "atc", "radar", "cockpit", "b737", "a320", "avionics", "tayara" } },
                { "simulators", new[] { "simulation", "flight", "pilot", "atc", "radar", "cockpit", "b737", "a320", "avionics", "tayara" } },
                { "exam", new[] { "result", "results", "marks", "grade", "grades", "calendar", "schedule", "datesheet" } },
                { "result", new[] { "results", "exam", "marks", "grades", "transcript", "portal" } },
                { "scholarship", new[] { "scholarships", "financial aid", "waiver", "discount", "merit", "concession", "wazifa" } },
                { "scholarships", new[] { "scholarship", "financial aid", "waiver", "discount", "merit", "concession", "wazifa" } },
                { "food", new[] { "mess", "cafeteria", "canteen", "dining", "meal", "khana" } },
                { "khana", new[] { "mess", "food", "cafeteria", "dining", "meal" } },
                { "sports", new[] { "gym", "cricket", "football", "fitness", "ground", "games", "tennis" } },
                { "library", new[] { "books", "reading", "study", "e-library", "catalog", "kitaben" } },
                { "it", new[] { "cyber", "computer", "software", "computing", "technology", "ai", "artificial intelligence" } },
                { "engineering", new[] { "be", "avionics", "aerospace", "airport", "ans", "technology" } }
            };

            foreach (var token in tokens)
            {
                if (synMap.TryGetValue(token, out var mapped))
                {
                    result.AddRange(mapped);
                }
            }

            return result.Distinct().ToList();
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var noScripts = Regex.Replace(html, @"<(script|style)[^>]*?>.*?</\1>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var withBreaks = Regex.Replace(noScripts, @"<(br|p|div|h[1-6]|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
            var plain = Regex.Replace(withBreaks, @"<[^>]*>", " ");
            plain = System.Net.WebUtility.HtmlDecode(plain);
            plain = Regex.Replace(plain, @"[ \t]+", " ");
            plain = Regex.Replace(plain, @"\n\s*\n+", "\n");

            return plain.Trim();
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + "...";
        }
    }
}

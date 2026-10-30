using System.Collections.Generic;

namespace NAU_Web.Models
{
    public static class AppPermissions
    {
        // 1. System & Administration
        public const string AdminFullAccess = "Admin.FullAccess";
        public const string AdminManageUsers = "Admin.ManageUsers";
        public const string AdminManageSettings = "Admin.ManageSettings";
        public const string AdminAnnouncement = "Admin.Announcement";
        public const string AdminChatbot = "Admin.Chatbot";
        public const string AdminAnalyticsAndHealth = "Admin.AnalyticsAndHealth";

        // 2. Campus Facilities & Student Life
        public const string ContentFacilities = "Content.Facilities"; // Master Facilities Overview
        public const string FacilityLibrary = "Facility.Library";
        public const string FacilityCafeteria = "Facility.Cafeteria";
        public const string FacilityHostel = "Facility.Hostel";
        public const string FacilityMess = "Facility.Mess";
        public const string FacilitySports = "Facility.Sports";
        public const string FacilityMedical = "Facility.Medical";
        public const string FacilityMasjid = "Facility.Masjid";
        public const string FacilitySmartCampus = "Facility.SmartCampus";

        // 3. Simulation & Aviation Laboratories
        public const string ContentSimulationLabs = "Content.SimulationLabs"; // Master Sim Labs Overview
        public const string SimFlight = "Sim.Flight";
        public const string SimATC = "Sim.ATC";
        public const string SimAvionics = "Sim.Avionics";
        public const string SimNavigation = "Sim.Navigation";

        // 4. Professional Training Schools
        public const string SchoolAll = "School.All";
        public const string SchoolANS = "School.ANS";
        public const string SchoolAirport = "School.Airport";
        public const string SchoolRegulatory = "School.Regulatory";
        public const string SchoolManagement = "School.Management";
        public const string ContentCourseCatalog = "Content.CourseCatalog";

        // 5. Academic Faculties (Degree Programs)
        public const string FacultyAll = "Faculty.All";
        public const string FacultyEngineering = "Faculty.Engineering";
        public const string FacultyIT = "Faculty.IT";
        public const string FacultyOperations = "Faculty.Operations";
        public const string FacultyManagement = "Faculty.Management";

        // 6. Website Content & Dynamic Modules
        public const string ContentHomepage = "Content.Homepage";
        public const string ContentNewsAndMedia = "Content.NewsAndMedia";
        public const string ContentGallery = "Content.Gallery";
        public const string ContentLinks = "Content.Links";
        public const string ContentFinancial = "Content.Financial";
        public const string ContentAbout = "Content.About";
        public const string ContentContact = "Content.Contact";
        public const string ContentApplications = "Content.Applications";
        public const string ContentCustomPages = "Content.CustomPages";
        public const string ContentSearch = "Content.Search";

        public static readonly List<PermissionGroupDto> AllGroups = new()
        {
            new PermissionGroupDto
            {
                GroupName = "System & Administrative Control",
                Icon = "fas fa-shield-halved",
                Color = "#0b2c4d",
                Permissions = new List<PermissionItemDto>
                {
                    new(AdminFullAccess, "Super Admin Full Access", "Unrestricted authority across all modules, pages, facilities, and settings"),
                    new(AdminManageUsers, "User & Role Management", "Create, edit, delete user accounts and assign granular authority permissions"),
                    new(AdminManageSettings, "Site Branding & Navigation", "Edit university logo, dark mode branding, tagline, and top navigation items"),
                    new(AdminAnnouncement, "Announcement Bar & Banners", "Create, edit, and toggle top alert announcement banner and active alerts"),
                    new(AdminAnalyticsAndHealth, "Analytics & System Health", "View real-time visitor traffic telemetry, server uptime, and diagnostics")
                }
            },
            new PermissionGroupDto
            {
                GroupName = "Campus Facilities & Student Life",
                Icon = "fas fa-building-columns",
                Color = "#0284c7",
                Permissions = new List<PermissionItemDto>
                {
                    new(ContentFacilities, "All Campus Facilities (Full Access)", "Master access to edit main facilities overview and all facility pages"),
                    new(FacilityLibrary, "Central Library & Digital Hub", "Edit Central Library facilities, catalog search links, hours, and digital archives"),
                    new(FacilityCafeteria, "Cafeteria & Dining Hall", "Edit student cafeteria details, food court menus, hygiene standards, and specs"),
                    new(FacilityHostel, "Student Hostels & Lodging", "Edit residential hostels, room capacity, amenities, and hostel rules"),
                    new(FacilityMess, "Officers & Cadet Dining Mess", "Edit officers mess, meal schedules, dining options, and mess governance"),
                    new(FacilitySports, "Sports Complex & Gymnasium", "Edit sports grounds, indoor gymnasium, athletic courts, and sports facilities"),
                    new(FacilityMedical, "Medical Center & First Aid", "Edit campus health center, emergency medical services, and doctor schedules"),
                    new(FacilityMasjid, "University Campus Masjid", "Edit campus central masjid, prayer timing schedules, and facility details"),
                    new(FacilitySmartCampus, "Smart Campus & Wi-Fi Network", "Edit high-speed campus Wi-Fi, IT infrastructure, and smart campus specs")
                }
            },
            new PermissionGroupDto
            {
                GroupName = "Simulation & Aviation Laboratories",
                Icon = "fas fa-vr-cardboard",
                Color = "#d97706",
                Permissions = new List<PermissionItemDto>
                {
                    new(ContentSimulationLabs, "All Simulation Labs (Full Access)", "Master access to edit simulation overview and all simulator lab pages"),
                    new(SimFlight, "Aerodrome Flight Simulators", "Edit Full Motion & Fixed-Base Flight Simulators, cockpit specs, and booking info"),
                    new(SimATC, "Air Traffic Control (ATC) Simulators", "Edit ATC Tower & Radar Simulator suites, 3D visual system, and training specs"),
                    new(SimAvionics, "Avionics & Micro-Electronics Lab", "Edit Avionics test benches, radar transmitter labs, and hardware equipment"),
                    new(SimNavigation, "Navigation Aids & Radar Operations Lab", "Edit ILS, VOR, DME calibration simulators and radar operations lab details")
                }
            },
            new PermissionGroupDto
            {
                GroupName = "Professional Training Schools",
                Icon = "fas fa-school",
                Color = "#1e8e3e",
                Permissions = new List<PermissionItemDto>
                {
                    new(SchoolAll, "All Schools (Full Training Access)", "Manage courses, schedules, and ATP records across all 4 professional schools"),
                    new(SchoolANS, "School of Air Navigation Services (ANS)", "Manage ATC, Radar/ATSEP, Search & Rescue (SAR), ELP, and ANS training courses"),
                    new(SchoolAirport, "School of Airport Services", "Manage Aerodrome Firefighting (ARFF), Ground Handling & Airport Operations courses"),
                    new(SchoolRegulatory, "School of Regulatory Services", "Manage Aviation Safety Management (SMS), Auditing, and Regulatory compliance"),
                    new(SchoolManagement, "School of Aviation Management", "Manage Executive Aviation Leadership, Airline Economics, and Airport Management"),
                    new(ContentCourseCatalog, "Course Catalog & Calendar (ATP 2026)", "Edit course catalog items, ATP calendar schedules, course fees, and timelines")
                }
            },
            new PermissionGroupDto
            {
                GroupName = "Academic Faculties (Degree Programs)",
                Icon = "fas fa-graduation-cap",
                Color = "#1a5fa6",
                Permissions = new List<PermissionItemDto>
                {
                    new(FacultyAll, "All Academic Faculties", "Manage degree programs, curriculum, and faculty details across all 4 academic faculties"),
                    new(FacultyEngineering, "Faculty of Engineering", "Manage BE ANS Engineering and BE Airport Engineering degree programs"),
                    new(FacultyIT, "Faculty of IT & Cyber Security", "Manage BS Aviation Cyber Security and IT degree programs and curriculum"),
                    new(FacultyOperations, "Faculty of Aviation Operations", "Manage BS Aviation Management and Aviation Operations degree programs"),
                    new(FacultyManagement, "Faculty of Management Sciences", "Manage MBA Aviation Management regular and executive degree programs")
                }
            },
            new PermissionGroupDto
            {
                GroupName = "Website Content & Public Modules",
                Icon = "fas fa-globe",
                Color = "#7c3aed",
                Permissions = new List<PermissionItemDto>
                {
                    new(ContentHomepage, "Homepage & Hero Slider Editor", "Edit homepage hero slides, welcome message, interactive feature cards, and banner images"),
                    new(ContentNewsAndMedia, "News & Media Feed", "Publish, edit, and archive news posts, press releases, and media announcements"),
                    new(ContentGallery, "Photo & Video Media Gallery", "Edit campus photo albums, video gallery, and event media assets"),
                    new(ContentLinks, "Useful Links & Student Portals", "Add, edit, or remove student/staff portal links, LMS, and E-library links"),
                    new(ContentFinancial, "Financial & Fee Structure", "Edit semester fee structures, scholarships, payment methods, and financial aid info"),
                    new(ContentAbout, "About Us & History Sections", "Edit History of CAA, History of CATI, Management structure, and Governance"),
                    new(ContentContact, "Contact Information & Location", "Edit university address, emergency phone numbers, contact emails, and map location"),
                    new(ContentApplications, "Admissions & Online Applications", "View and review student registrations, admission applications, and submissions"),
                    new(ContentCustomPages, "Custom CMS Pages & Dynamic Routes", "Create, edit, and publish dynamic custom CMS pages and custom routes"),
                    new(ContentSearch, "Global Website Search Index", "Manage website search index, portal search parameters, and keyword tagging")
                }
            }
        };
    }

    public class PermissionGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-layer-group";
        public string Color { get; set; } = "#0b2c4d";
        public List<PermissionItemDto> Permissions { get; set; } = new();
    }

    public class PermissionItemDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public PermissionItemDto() { }
        public PermissionItemDto(string key, string title, string description)
        {
            Key = key;
            Title = title;
            Description = description;
        }
    }
}

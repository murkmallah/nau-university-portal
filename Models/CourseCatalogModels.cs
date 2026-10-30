using System.Collections.Generic;

namespace NAU_Web.Models
{
    public class CourseCatalogHero
    {
        public string Eyebrow { get; set; } = "ATP 2026";
        public string Title { get; set; } = "NAU Course Catalog";
        public string TitleEm { get; set; } = "Course Catalog";
        public string Description { get; set; } = "Browse the complete Academic Training Plan 2026 — hundreds of certified courses across Air Navigation Services, Regulatory Services, Airport Services and Aviation Management.";
    }

    public class CourseCatalogItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Duration { get; set; } = "01 Week";
        public string StartDate { get; set; } = "TBD";
        public string EndDate { get; set; } = "TBD";
        public string? StatusOverride { get; set; }
        public string? Note { get; set; } = "Part of NAU – ATP 2026 Academic Training Plan, Hyderabad";
    }

    public class CourseCatalogSchool
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Icon { get; set; } = "fa-tower-cell";
        public string Color { get; set; } = "#0b2c4d";
        public List<CourseCatalogItem> Courses { get; set; } = new();
    }

    public class CourseCatalogStat
    {
        public string Number { get; set; } = "300+";
        public string Label { get; set; } = "Total Courses";
    }

    public class CourseCatalogCta
    {
        public string Heading { get; set; } = "Ready to Advance Your Aviation Career?";
        public string Subheading { get; set; } = "Enroll in a certified course and earn a recognized qualification from National Aviation University.";
        public string ButtonText { get; set; } = "Register Now";
        public string ButtonUrl { get; set; } = "/Account/Register";
    }

    public class CourseCatalogPageContent
    {
        public CourseCatalogHero Hero { get; set; } = new();
        public List<CourseCatalogSchool> Schools { get; set; } = new();
        public List<CourseCatalogStat> Stats { get; set; } = new();
        public CourseCatalogCta Cta { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NAU_Web.Models
{
    [Table("FooterSettings")]
    public class FooterSetting
    {
        [Key]
        public int Id { get; set; }

        public string? AboutText { get; set; }

        public string? ContactAddress { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactWebsite { get; set; }

        public string? CopyrightText { get; set; }

        public string? SocialLinksJson { get; set; }
        public string? QuickLinksJson { get; set; }
        public string? BottomLinksJson { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class FooterSocialLinkItem
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "fas fa-link";
        public string Url { get; set; } = "#";
        public string Target { get; set; } = "_blank";
    }

    public class FooterLinkItem
    {
        public string Text { get; set; } = "";
        public string Url { get; set; } = "#";
        public string Target { get; set; } = "_self";
    }

    public class FooterColumnItem
    {
        public string ColumnTitle { get; set; } = "";
        public List<FooterLinkItem> Links { get; set; } = new();
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NAU_Web.Models
{
    [Table("SiteBrandingSettings")]
    public class SiteBrandingSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string SiteTitle { get; set; } = "National Aviation University";

        [MaxLength(200)]
        public string Tagline { get; set; } = "LEARN · LEAD · FLY HIGH";

        [Required]
        [MaxLength(500)]
        public string LogoUrl { get; set; } = "/images/logo.png";

        [MaxLength(500)]
        public string? DarkModeLogoUrl { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

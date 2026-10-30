using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NAU_Web.Models
{
    [Table("AnnouncementSettings")]
    public class AnnouncementSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TagText { get; set; } = "ANNOUNCEMENT:";

        [Required]
        [MaxLength(300)]
        public string MessageText { get; set; } = "Admissions Open Now";

        [MaxLength(300)]
        public string? LinkUrl { get; set; }

        public bool IsVisible { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

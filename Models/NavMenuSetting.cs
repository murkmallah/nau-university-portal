using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NAU_Web.Models
{
    [Table("NavMenuSettings")]
    public class NavMenuSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string MenuKey { get; set; } = null!; // e.g. "menu_academics", "col_academics_faculties", "col_academics_schools", "item_fac_engineering"

        [MaxLength(100)]
        public string? ParentKey { get; set; } // e.g. "menu_academics" or "col_academics_faculties"

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Menu"; // "Menu", "Column", "Link", "Card"

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = null!; // Display title e.g. "Academics", "Faculties", "School of ANS"

        [MaxLength(250)]
        public string? Description { get; set; } // Short description/subtitle

        public bool IsHidden { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

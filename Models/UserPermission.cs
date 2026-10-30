using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NAU_Web.Models
{
    public class UserPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string PermissionKey { get; set; } = string.Empty;

        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

        public int? GrantedByUserId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NAU_Web.Models
{
    // Data submitted from the public Contact Us page (Views/Contact/Index.cshtml).
    public class ContactMessageViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [StringLength(30)]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Message { get; set; }

        // Honeypot anti-spam field: left empty by real visitors, filled in by
        // most spam bots. Kept off-screen in the view -- if it arrives
        // non-empty, the controller silently drops the submission.
        public string? Website { get; set; }
    }
}

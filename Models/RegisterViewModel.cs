using System.ComponentModel.DataAnnotations;

namespace NAU_Web.Models
{
    public class RegisterViewModel
    {
        // Personal Information
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 50 characters.")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 50 characters.")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Date of birth is required.")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "CNIC / B-Form number is required.")]
        [RegularExpression(@"^\d{5}-\d{7}-\d$", ErrorMessage = "CNIC must be in the format 12345-1234567-1.")]
        public string Cnic { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+92|0)[0-9]{10}$", ErrorMessage = "Enter a valid phone number, e.g. +923000000000 or 03000000000.")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Residential address is required.")]
        [StringLength(250, MinimumLength = 10, ErrorMessage = "Address must be at least 10 characters long.")]
        public string Address { get; set; } = "";

        public string? City { get; set; }
        public string? Province { get; set; }

        // Account Setup
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";

        public bool TermsAccepted { get; set; }
        public bool InfoConfirmed { get; set; }

        // NOTE: Academic Background and Program Selection fields were removed from the
        // Register form per requirements (personal + account info only). These properties
        // are intentionally kept (unused, defaulted) so existing Application records /
        // Admin views that read academic & program data on Application entities keep working.
        public string HighestQualification { get; set; } = "";
        public string BoardOrUniversity { get; set; } = "";
        public int PassingYear { get; set; }
        public int? ObtainedMarks { get; set; }
        public int? TotalMarks { get; set; }
        public string? SubjectGroup { get; set; }
        public string? PreviousSchool { get; set; }
        public string? Certifications { get; set; }
        public string DesiredProgram { get; set; } = "";
        public string IntakeSession { get; set; } = "";
        public string? ShiftPreference { get; set; }
        public string? HowHeard { get; set; }
        public string? WhyJoin { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NAU_Web.Models
{
    public class Application
    {
        public int ApplicationId { get; set; }
        public int UserId { get; set; }

        // Step 1
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string Cnic { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? City { get; set; }
        public string? Province { get; set; }

        // Step 2
        public string HighestQualification { get; set; } = null!;
        public string BoardOrUniversity { get; set; } = null!;
        public int PassingYear { get; set; }
        public int? ObtainedMarks { get; set; }
        public int? TotalMarks { get; set; }
        public string? SubjectGroup { get; set; }
        public string? PreviousSchool { get; set; }
        public string? Certifications { get; set; }

        // Step 3
        public string DesiredProgram { get; set; } = null!;
        public string IntakeSession { get; set; } = null!;
        public string? ShiftPreference { get; set; }
        public string? HowHeard { get; set; }
        public string? WhyJoin { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }

        public string Status { get; set; } = "Submitted";
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public virtual ApplicationUser User { get; set; } = null!;
    }
}
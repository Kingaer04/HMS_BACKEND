using HMS.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace HMS.Entities.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public Guid? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }

        // Extended profiles — only one set depending on Role
        public DoctorProfile? DoctorProfile { get; set; }
        public ReceptionistProfile? ReceptionistProfile { get; set; }
        public LabTechnicianProfile? LabTechnicianProfile { get; set; }

        // Only set if Role == Patient
        public Patient? PatientRecord { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }
}
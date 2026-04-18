using HMS.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace HMS.Entities.Models
{
    public class Hospital
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HospitalUID { get; set; } = string.Empty; // NHIS-verified UID

        // Address
        public string BlockNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = "Nigeria";

        // Contact
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<ApplicationUser> Staff { get; set; } = new List<ApplicationUser>();
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<LabTestCatalogue> LabCatalogue { get; set; } = new List<LabTestCatalogue>();

        /// <summary>
        /// Patients that were REGISTERED HERE (origin hospital).
        /// Patients that merely VISIT here appear in HospitalVisit records.
        /// </summary>
        public ICollection<Patient> RegisteredPatients { get; set; } = new List<Patient>();
    }

    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Hospital this user belongs to (null only for system admins)
        public Guid? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        // JWT refresh token
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }

        // Extended profiles — only one will be set depending on Role
        public DoctorProfile? DoctorProfile { get; set; }
        public ReceptionistProfile? ReceptionistProfile { get; set; }
        public LabTechnicianProfile? LabTechnicianProfile { get; set; }

        // Set only if this user is a Patient (Role == Patient)
        public Patient? PatientRecord { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class NhisVerificationResult
    {
        public bool IsValid { get; set; }
        public string? HospitalName { get; set; }
        public string? Message { get; set; }
    }
}

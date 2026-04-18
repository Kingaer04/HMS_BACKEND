namespace HMS.Entities.Models
{
    /// <summary>
    /// Hospital department — e.g. Cardiology, Paediatrics, General.
    /// Doctors belong to departments, appointments can be made per department.
    /// </summary>
    public class Department
    {
        public Guid Id { get; set; }
        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<DoctorProfile> Doctors { get; set; } = new List<DoctorProfile>();
    }

    /// <summary>
    /// Extended profile for doctors — specialty, schedule, availability.
    /// Separate from ApplicationUser so it doesn't bloat the identity table.
    /// </summary>
    public class DoctorProfile
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string Specialization { get; set; } = string.Empty;
        public string MedicalLicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string? Qualifications { get; set; }     // MBBS, FMCP, etc.
        public string? Biography { get; set; }

        // ── Availability ──────────────────────────────────────────────
        /// <summary>
        /// JSON: { "Monday": ["09:00-12:00", "14:00-17:00"], "Tuesday": [...] }
        /// </summary>
        public string? AvailabilitySchedule { get; set; }

        public bool IsAvailableToday { get; set; } = true;
        public int MaxPatientsPerDay { get; set; } = 20;
        public int CurrentPatientCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extended profile for lab technicians.
    /// </summary>
    public class LabTechnicianProfile
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public string Specialization { get; set; } = string.Empty; // Haematology, Microbiology, etc.
        public string? LicenseNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extended profile for receptionists/nurses.
    /// </summary>
    public class ReceptionistProfile
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public string? NursingLicenseNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

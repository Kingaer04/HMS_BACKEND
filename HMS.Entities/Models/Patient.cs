using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// Central patient record.
    ///
    /// IDENTITY STRATEGY:
    /// - Every patient gets a system-generated HmsPatientId (e.g. HMS-2025-000001)
    /// - This is their universal identifier across all hospitals in the system
    /// - Patient is registered under one OriginHospital
    /// - They can visit any other hospital — receptionist searches by
    ///   HmsPatientId, full name, or phone number
    /// - NIN is NOT used — no external government database dependency
    /// - NHIS number is optional — only if patient presents it
    /// </summary>
    public class Patient
    {
        public Guid Id { get; set; }

        // ── HMS System ID ─────────────────────────────────────────────
        /// <summary>
        /// Human-readable unique ID generated on registration.
        /// Format: HMS-{YEAR}-{6-digit-sequence} → HMS-2025-000001
        /// Used by receptionists to pull up returning patients.
        /// Unique index across the entire system.
        /// </summary>
        public string HmsPatientId { get; set; } = string.Empty;

        // ── Personal Info ─────────────────────────────────────────────
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }
        public string Nationality { get; set; } = "Nigerian";

        // ── Optional Government ID ────────────────────────────────────
        /// <summary>
        /// NHIS number — optional, collected only if patient presents it.
        /// Used only for billing/insurance, not for identity.
        /// </summary>
        public string? NHISNumber { get; set; }

        // ── Contact ───────────────────────────────────────────────────
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }

        // ── Address ───────────────────────────────────────────────────
        public string? BlockNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Country { get; set; } = "Nigeria";

        // ── Emergency Contact ─────────────────────────────────────────
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelationship { get; set; }

        // ── Origin Hospital ───────────────────────────────────────────
        /// <summary>
        /// The hospital that first registered this patient.
        /// Stays the same forever — even if the patient visits 10 other hospitals.
        /// </summary>
        public Guid OriginHospitalId { get; set; }
        public Hospital? OriginHospital { get; set; }

        // ── Access Control ────────────────────────────────────────────
        public AccessLevel AccessLevel { get; set; } = AccessLevel.Open;

        // ── Patient App Account ───────────────────────────────────────
        /// <summary>
        /// Set only if the patient uses the patient app.
        /// Walk-in only patients who never download the app will have this as null.
        /// </summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        // ── Record Status ─────────────────────────────────────────────
        public RecordStatus RecordStatus { get; set; } = RecordStatus.Active;
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        // ── Computed Properties ───────────────────────────────────────
        public int Age => DateTime.UtcNow.Year - DateOfBirth.Year -
                          (DateTime.UtcNow.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

        public string FullName => string.IsNullOrEmpty(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";

        // ── Navigation ────────────────────────────────────────────────
        public MedicalRecord? MedicalRecord { get; set; }
        public PatientAccessControl? AccessControl { get; set; }
        public ICollection<HospitalVisit> Visits { get; set; } = new List<HospitalVisit>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}

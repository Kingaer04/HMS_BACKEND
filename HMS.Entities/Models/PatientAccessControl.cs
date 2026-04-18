using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// Controls how a patient's medical record is accessed.
    /// Patient can choose to lock their file — requiring a code or fingerprint
    /// before any hospital staff can view the record.
    /// </summary>
    public class PatientAccessControl
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        public AccessLevel AccessLevel { get; set; } = AccessLevel.Open;
        public UnlockMethod PreferredUnlockMethod { get; set; } = UnlockMethod.AccessCode;

        // ── Access Code ───────────────────────────────────────────────
        /// <summary>
        /// Hashed access code — never store plain text.
        /// Patient uses this to unlock their file at reception.
        /// </summary>
        public string? AccessCodeHash { get; set; }

        /// <summary>
        /// Short numeric code shown to staff to initiate unlock request.
        /// Separate from the full access code.
        /// </summary>
        public string? QuickReferenceCode { get; set; }

        // ── Biometric ─────────────────────────────────────────────────
        /// <summary>
        /// Fingerprint template hash — for future biometric integration.
        /// Stored as a reference/hash, not the raw biometric data.
        /// </summary>
        public string? FingerprintHash { get; set; }

        // ── Temporary Override ────────────────────────────────────────
        /// <summary>
        /// Emergency override — HospitalAdmin can grant temporary access
        /// to a locked file in a life-threatening situation.
        /// All overrides are logged.
        /// </summary>
        public bool EmergencyOverrideActive { get; set; } = false;
        public DateTime? EmergencyOverrideExpiry { get; set; }
        public string? EmergencyOverrideGrantedBy { get; set; } // UserId of admin

        // ── Audit ─────────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
        public DateTime? LastUnlockedAt { get; set; }
        public string? LastUnlockedBy { get; set; } // UserId

        // Navigation
        public ICollection<AccessLog> AccessLogs { get; set; } = new List<AccessLog>();
    }

    /// <summary>
    /// Every access attempt — successful or failed — is logged.
    /// </summary>
    public class AccessLog
    {
        public Guid Id { get; set; }
        public Guid PatientAccessControlId { get; set; }
        public PatientAccessControl? AccessControl { get; set; }

        public string AccessedByUserId { get; set; } = string.Empty;
        public string AccessedByName { get; set; } = string.Empty;
        public string AccessedByRole { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public bool WasSuccessful { get; set; }
        public bool WasEmergencyOverride { get; set; }
        public string? FailureReason { get; set; }
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    }
}

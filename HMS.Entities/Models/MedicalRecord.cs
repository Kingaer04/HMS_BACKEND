using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// The patient's master medical file.
    /// One record per patient — sections are updated by different roles.
    /// </summary>
    public class MedicalRecord
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        public RecordStatus Status { get; set; } = RecordStatus.Active;

        /// <summary>
        /// If this was a temp emergency record, stores the Id of the
        /// real record it was merged into.
        /// </summary>
        public Guid? MergedIntoRecordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // ── Sections (1-to-1 relationships) ──────────────────────────
        public VitalInfo? VitalInfo { get; set; }
        public AllergyInfo? AllergyInfo { get; set; }

        // ── Collections ───────────────────────────────────────────────
        public ICollection<DoctorNote> DoctorNotes { get; set; } = new List<DoctorNote>();
        public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<HospitalVisit> VisitHistory { get; set; } = new List<HospitalVisit>();
    }

    // ── Vital Info — updated by Receptionist/Nurse ───────────────────
    public class VitalInfo
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public BloodGroup BloodGroup { get; set; } = BloodGroup.Unknown;
        public string? Genotype { get; set; }           // AA, AS, SS, AC etc.
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public double? BMI => HeightCm.HasValue && WeightKg.HasValue && HeightCm > 0
            ? Math.Round(WeightKg.Value / Math.Pow(HeightCm.Value / 100, 2), 1)
            : null;

        // ── Per-visit vitals (latest reading) ────────────────────────
        public string? BloodPressure { get; set; }     // e.g. "120/80"
        public double? TemperatureCelsius { get; set; }
        public int? PulseRateBpm { get; set; }
        public int? RespiratoryRate { get; set; }
        public double? OxygenSaturation { get; set; }  // SpO2 %

        public string? LastUpdatedBy { get; set; }     // UserId
        public DateTime? LastUpdatedAt { get; set; }

        // Vital history is tracked in VitalReading
        public ICollection<VitalReading> VitalReadings { get; set; } = new List<VitalReading>();
    }

    /// <summary>
    /// Every time vitals are recorded we store a history entry.
    /// So the doctor can see trends, not just the latest reading.
    /// </summary>
    public class VitalReading
    {
        public Guid Id { get; set; }
        public Guid VitalInfoId { get; set; }
        public VitalInfo? VitalInfo { get; set; }

        public Guid VisitId { get; set; }              // Which visit this was recorded in

        public string? BloodPressure { get; set; }
        public double? TemperatureCelsius { get; set; }
        public int? PulseRateBpm { get; set; }
        public int? RespiratoryRate { get; set; }
        public double? OxygenSaturation { get; set; }
        public double? WeightKg { get; set; }

        public string RecordedByUserId { get; set; } = string.Empty;
        public string RecordedByName { get; set; } = string.Empty;
        public Guid RecordedAtHospitalId { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }

    // ── Allergy Info ─────────────────────────────────────────────────
    public class AllergyInfo
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public bool HasKnownAllergies { get; set; } = false;
        public string? AllergyDetails { get; set; }    // Free text: penicillin, peanuts, etc.
        public string? ChronicConditions { get; set; } // Diabetes, hypertension, etc.
        public string? CurrentMedications { get; set; }
        public string? SurgicalHistory { get; set; }
        public string? FamilyHistory { get; set; }

        public string? LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    // ── Doctor Notes — only visible to doctors ────────────────────────
    /// <summary>
    /// Written by the assigned doctor during or after a consultation.
    /// Visible ONLY to doctors — hidden from receptionist, lab, patient.
    /// </summary>
    public class DoctorNote
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public Guid VisitId { get; set; }
        public HospitalVisit? Visit { get; set; }

        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public string HospitalName { get; set; } = string.Empty;

        public string ChiefComplaint { get; set; } = string.Empty;   // Why patient came
        public string History { get; set; } = string.Empty;           // History of presenting complaint
        public string Examination { get; set; } = string.Empty;       // Physical exam findings
        public string Diagnosis { get; set; } = string.Empty;         // Doctor's diagnosis
        public string TreatmentPlan { get; set; } = string.Empty;     // What doctor prescribed/ordered
        public string? FollowUpInstructions { get; set; }
        public string? Referral { get; set; }                         // Referred to specialist?
        public bool IsConfidential { get; set; } = true;              // Always true — doctor-eyes only

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        // Doctor can update their own notes within 24hrs of writing
        public bool IsEditable =>
            (DateTime.UtcNow - CreatedAt).TotalHours <= 24;
    }

    // ── Prescriptions ─────────────────────────────────────────────────
    public class Prescription
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public Guid VisitId { get; set; }
        public string DoctorUserId { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }

        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;          // e.g. "500mg"
        public string Frequency { get; set; } = string.Empty;       // e.g. "Twice daily"
        public string Duration { get; set; } = string.Empty;        // e.g. "7 days"
        public string? Instructions { get; set; }                    // "Take after meals"
        public bool IsActive { get; set; } = true;

        public DateTime PrescribedAt { get; set; } = DateTime.UtcNow;
    }
}

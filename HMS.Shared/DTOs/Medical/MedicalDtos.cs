using System.ComponentModel.DataAnnotations;
using HMS.Entities.Enums;

namespace HMS.Shared.DTOs.Medical
{
    // ── Vitals ────────────────────────────────────────────────────────
    public class UpdateVitalsDto
    {
        public string? BloodPressure { get; set; }
        public double? TemperatureCelsius { get; set; }
        public int? PulseRateBpm { get; set; }
        public int? RespiratoryRate { get; set; }
        public double? OxygenSaturation { get; set; }
        public double? WeightKg { get; set; }
        public double? HeightCm { get; set; }
        public BloodGroup? BloodGroup { get; set; }
        public string? Genotype { get; set; }
    }

    // ── Doctor Notes ──────────────────────────────────────────────────
    public class AddDoctorNoteDto
    {
        [Required] public string ChiefComplaint { get; set; } = string.Empty;
        [Required] public string History { get; set; } = string.Empty;
        [Required] public string Examination { get; set; } = string.Empty;
        [Required] public string Diagnosis { get; set; } = string.Empty;
        [Required] public string TreatmentPlan { get; set; } = string.Empty;
        public string? FollowUpInstructions { get; set; }
        public string? Referral { get; set; }
    }

    public class UpdateDoctorNoteDto
    {
        public string? ChiefComplaint { get; set; }
        public string? History { get; set; }
        public string? Examination { get; set; }
        public string? Diagnosis { get; set; }
        public string? TreatmentPlan { get; set; }
        public string? FollowUpInstructions { get; set; }
        public string? Referral { get; set; }
    }

    // ── Allergy Info ──────────────────────────────────────────────────
    public class UpdateAllergyInfoDto
    {
        public bool HasKnownAllergies { get; set; }
        public string? AllergyDetails { get; set; }
        public string? ChronicConditions { get; set; }
        public string? CurrentMedications { get; set; }
        public string? SurgicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
    }

    // ── Prescription ──────────────────────────────────────────────────
    public class AddPrescriptionDto
    {
        [Required] public string MedicationName { get; set; } = string.Empty;
        [Required] public string Dosage { get; set; } = string.Empty;
        [Required] public string Frequency { get; set; } = string.Empty;
        [Required] public string Duration { get; set; } = string.Empty;
        public string? Instructions { get; set; }
    }

    // ── Response DTOs ─────────────────────────────────────────────────
    public class MedicalRecordDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string HmsPatientId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Sections
        public VitalInfoDto? Vitals { get; set; }
        public AllergyInfoDto? AllergyInfo { get; set; }

        // Doctor notes — only included if requestor is a Doctor
        public List<DoctorNoteDto>? DoctorNotes { get; set; }

        public List<PrescriptionDto> Prescriptions { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class VitalInfoDto
    {
        public string? BloodGroup { get; set; }
        public string? Genotype { get; set; }
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public double? BMI { get; set; }
        public string? BloodPressure { get; set; }
        public double? TemperatureCelsius { get; set; }
        public int? PulseRateBpm { get; set; }
        public int? RespiratoryRate { get; set; }
        public double? OxygenSaturation { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<VitalReadingDto> ReadingHistory { get; set; } = new();
    }

    public class VitalReadingDto
    {
        public string? BloodPressure { get; set; }
        public double? TemperatureCelsius { get; set; }
        public int? PulseRateBpm { get; set; }
        public double? WeightKg { get; set; }
        public string RecordedBy { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }

    public class AllergyInfoDto
    {
        public bool HasKnownAllergies { get; set; }
        public string? AllergyDetails { get; set; }
        public string? ChronicConditions { get; set; }
        public string? CurrentMedications { get; set; }
        public string? SurgicalHistory { get; set; }
        public string? FamilyHistory { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }

    public class DoctorNoteDto
    {
        public Guid Id { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string HospitalName { get; set; } = string.Empty;
        public string ChiefComplaint { get; set; } = string.Empty;
        public string History { get; set; } = string.Empty;
        public string Examination { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public string? FollowUpInstructions { get; set; }
        public string? Referral { get; set; }
        public bool IsEditable { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    public class PrescriptionDto
    {
        public Guid Id { get; set; }
        public string MedicationName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime PrescribedAt { get; set; }
    }

    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using HMS.Entities.Enums;

namespace HMS.Shared.DTOs.Patient
{
    // ── Register ──────────────────────────────────────────────────────
    public class RegisterPatientDto
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }

        [Required] public DateTime DateOfBirth { get; set; }
        [Required] public Gender Gender { get; set; }
        public MaritalStatus MaritalStatus { get; set; }

        [Required][Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Phone] public string? AlternativePhone { get; set; }
        [EmailAddress] public string? Email { get; set; }

        // Address
        public string? BlockNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }

        // Emergency contact
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelationship { get; set; }

        // Optional NHIS
        public string? NHISNumber { get; set; }
    }

    public class UpdatePatientDto
    {
        public string? MiddleName { get; set; }
        [Phone] public string? PhoneNumber { get; set; }
        [Phone] public string? AlternativePhone { get; set; }
        [EmailAddress] public string? Email { get; set; }
        public string? BlockNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string? NHISNumber { get; set; }
        public MaritalStatus? MaritalStatus { get; set; }
    }

    // ── Access Control ────────────────────────────────────────────────
    public class LockRecordDto
    {
        [Required] public string AccessCode { get; set; } = string.Empty;
        [Required][Compare("AccessCode")] public string ConfirmAccessCode { get; set; } = string.Empty;
        public UnlockMethod PreferredMethod { get; set; } = UnlockMethod.AccessCode;
    }

    public class UnlockRecordDto
    {
        [Required] public string AccessCode { get; set; } = string.Empty;
    }

    // ── Temp / Emergency ──────────────────────────────────────────────
    public class CreateTempRecordDto
    {
        public string? EstimatedFirstName { get; set; }
        public string? EstimatedLastName { get; set; }
        public string? EstimatedAge { get; set; }
        public string? EstimatedGender { get; set; }
        public string? PhysicalDescription { get; set; }
        public string? ItemsFoundOnPatient { get; set; }
        public string? CompanionName { get; set; }
        public string? CompanionPhone { get; set; }
        public string? CompanionRelationship { get; set; }
        public string? ChiefComplaint { get; set; }
    }

    // ── Response DTOs ─────────────────────────────────────────────────
    public class PatientSummaryDto
    {
        public Guid Id { get; set; }
        public string HmsPatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string OriginHospitalName { get; set; } = string.Empty;
        public AccessLevel AccessLevel { get; set; }
        public bool IsVisitingPatient { get; set; } // Not from this hospital
    }

    public class PatientDetailDto
    {
        public Guid Id { get; set; }
        public string HmsPatientId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhone { get; set; }
        public string? Email { get; set; }
        public string? NHISNumber { get; set; }
        public string? BlockNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? EmergencyContactRelationship { get; set; }
        public string OriginHospitalName { get; set; } = string.Empty;
        public Guid OriginHospitalId { get; set; }
        public AccessLevel AccessLevel { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    public class TempRecordDto
    {
        public Guid Id { get; set; }
        public string? EstimatedFirstName { get; set; }
        public string? EstimatedLastName { get; set; }
        public string? EstimatedAge { get; set; }
        public string? CompanionName { get; set; }
        public string? CompanionPhone { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsMerged { get; set; }
    }
}

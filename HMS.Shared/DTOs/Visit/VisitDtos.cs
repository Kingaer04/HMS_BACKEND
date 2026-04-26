using System.ComponentModel.DataAnnotations;
using HMS.Entities.Enums;

namespace HMS.Shared.DTOs.Visit
{
    public class CheckInDto
    {
        [Required] public Guid PatientId { get; set; }
        public string? ChiefComplaintOnArrival { get; set; }
        public bool IsEmergency { get; set; } = false;
        public AppointmentType VisitType { get; set; } = AppointmentType.WalkIn;
        public Guid? AppointmentId { get; set; }
    }

    public class AssignDoctorDto
    {
        [Required] public string DoctorUserId { get; set; } = string.Empty;
        [Required] public string DoctorName { get; set; } = string.Empty;
    }

    public class CheckOutDto
    {
        public string? DischargeNotes { get; set; }
        public string? FollowUpDate { get; set; }
    }

    // ── Response DTOs ─────────────────────────────────────────────────
    public class VisitDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string HmsPatientId { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string VisitType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public string? ChiefComplaintOnArrival { get; set; }
        public bool IsEmergency { get; set; }
        public string? AssignedDoctorName { get; set; }
        public string? AssignedDoctorId { get; set; }
        public DateTime? DoctorAssignedAt { get; set; }
        public DateTime? DoctorGrantedEntryAt { get; set; }
        public DateTime? ConsultationStartedAt { get; set; }
        public DateTime? ConsultationEndedAt { get; set; }
        public string? DischargeNotes { get; set; }
        public string? FollowUpDate { get; set; }
        public bool LabReferralMade { get; set; }
        public bool WasTemporaryRecord { get; set; }
    }

    public class VisitSummaryDto
    {
        public Guid Id { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string HmsPatientId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string VisitType { get; set; } = string.Empty;
        public DateTime CheckInTime { get; set; }
        public bool IsEmergency { get; set; }
        public string? AssignedDoctorName { get; set; }
        public string? ChiefComplaint { get; set; }
    }

    public class DoctorSummaryDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string? Department { get; set; }
        public bool IsAvailableToday { get; set; }
        public int CurrentPatientCount { get; set; }
        public int MaxPatientsPerDay { get; set; }
    }

    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DoctorCount { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using HMS.Entities.Enums;

// ── Lab ───────────────────────────────────────────────────────────────
namespace HMS.Shared.DTOs.Lab
{
    public class CreateLabRequestDto
    {
        [Required] public List<string> TestNames { get; set; } = new();
        public string? ClinicalNotes { get; set; }
    }

    public class UpdateLabResultDto
    {
        [Required] public LabTestStatus Status { get; set; }
        public DateTime? SampleCollectedAt { get; set; }
    }

    public class AddTestResultDto
    {
        [Required] public string TestName { get; set; } = string.Empty;
        [Required] public string TestCode { get; set; } = string.Empty;
        public string? Result { get; set; }
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool? IsAbnormal { get; set; }
        public string? Notes { get; set; }
    }

    public class AddLabCatalogueItemDto
    {
        [Required] public string TestName { get; set; } = string.Empty;
        [Required] public string TestCode { get; set; } = string.Empty;
        [Required] public string Category { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ExpectedTurnaroundTime { get; set; }
    }

    public class LabRequestDto
    {
        public Guid Id { get; set; }
        public string RequestedByDoctorName { get; set; } = string.Empty;
        public string? ClinicalNotes { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ResultReadyAt { get; set; }
        public string? ProcessedByLabTechName { get; set; }
        public List<LabTestResultDto> TestResults { get; set; } = new();
    }

    public class LabTestResultDto
    {
        public Guid Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string? Result { get; set; }
        public string? Unit { get; set; }
        public string? ReferenceRange { get; set; }
        public bool? IsAbnormal { get; set; }
        public string? Notes { get; set; }
        public DateTime? EnteredAt { get; set; }
    }

    public class LabCatalogueItemDto
    {
        public Guid Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ExpectedTurnaroundTime { get; set; }
        public bool IsActive { get; set; }
    }
}

// ── Appointment ───────────────────────────────────────────────────────
namespace HMS.Shared.DTOs.Appointment
{
    public class RequestAppointmentDto
    {
        [Required] public Guid HospitalId { get; set; }
        public string? PreferredDoctorId { get; set; }
        [Required] public DateTime RequestedDateTime { get; set; }
        public AppointmentType Type { get; set; } = AppointmentType.Scheduled;
        public string? ReasonForAppointment { get; set; }
    }

    public class ReceptionistReviewDto
    {
        [Required] public bool Approved { get; set; }
        public string? Note { get; set; }
        public string? AssignedDoctorId { get; set; }
        public string? AssignedDoctorName { get; set; }
    }

    public class DoctorRespondDto
    {
        [Required] public bool Approved { get; set; }
        public string? Note { get; set; }
        public DateTime? ConfirmedDateTime { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class AppointmentDto
    {
        public Guid Id { get; set; }
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string HmsPatientId { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public string? PreferredDoctorName { get; set; }
        public string? AssignedDoctorName { get; set; }
        public DateTime RequestedDateTime { get; set; }
        public DateTime? ConfirmedDateTime { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReasonForAppointment { get; set; }
        public bool? ReceptionistApproved { get; set; }
        public string? ReceptionistNote { get; set; }
        public bool? DoctorApproved { get; set; }
        public string? DoctorNote { get; set; }
        public string? RejectionReason { get; set; }
        public bool BookedViaApp { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

// ── Payment ───────────────────────────────────────────────────────────
namespace HMS.Shared.DTOs.Payment
{
    public class CreatePaymentDto
    {
        public decimal ConsultationFee { get; set; }
        public decimal LabFees { get; set; }
        public decimal MedicationFees { get; set; }
        public decimal OtherFees { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal NHISCoverage { get; set; } = 0;
        public List<PaymentLineItemDto> LineItems { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class ProcessPaymentDto
    {
        [Required] public PaymentMethod Method { get; set; }
        [Required] public decimal AmountPaid { get; set; }
        public string? TransactionReference { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public string? NHISNumber { get; set; }
    }

    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid VisitId { get; set; }
        public decimal ConsultationFee { get; set; }
        public decimal LabFees { get; set; }
        public decimal MedicationFees { get; set; }
        public decimal OtherFees { get; set; }
        public decimal Discount { get; set; }
        public decimal NHISCoverage { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Method { get; set; }
        public string? TransactionReference { get; set; }
        public string ProcessedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<PaymentLineItemDto> LineItems { get; set; } = new();
    }

    public class PaymentLineItemDto
    {
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Total { get; set; }
    }
}

// ── Notification ──────────────────────────────────────────────────────
namespace HMS.Shared.DTOs.Notification
{
    using HMS.Entities.Enums;

    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public Guid? RelatedPatientId { get; set; }
        public Guid? RelatedVisitId { get; set; }
        public Guid? RelatedAppointmentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool WasDeliveredRealTime { get; set; }
    }

    public enum NotificationType
    {
        PatientAssigned,
        AppointmentRequest,
        AppointmentConfirmed,
        AppointmentRejected,
        LabResultReady,
        PatientCheckedOut,
        EmergencyAlert,
        AccessCodeRequested,
        SystemAlert
    }
}

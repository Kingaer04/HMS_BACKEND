using HMS.Shared.DTOs.Patient;
using HMS.Shared.DTOs.Visit;
using HMS.Shared.DTOs.Medical;
using HMS.Shared.DTOs.Lab;
using HMS.Shared.DTOs.Appointment;
using HMS.Shared.DTOs.Payment;
using HMS.Shared.DTOs.Notification;
using HMS.Shared.Responses;
using HMS.Entities.Enums;

namespace HMS.Service.Contracts
{
    // ── Patient ───────────────────────────────────────────────────────
    public interface IPatientService
    {
        /// <summary>Search patients by HmsPatientId, name, or phone.</summary>
        Task<ApiResponse<List<PatientSummaryDto>>> SearchPatientsAsync(string query, Guid hospitalId);

        /// <summary>Get a patient's full profile — caller must have access.</summary>
        Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(Guid patientId, string requestingUserId);

        /// <summary>Register a brand new patient at this hospital (origin).</summary>
        Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(RegisterPatientDto dto, Guid hospitalId, string receptionistId);

        /// <summary>Update patient personal info.</summary>
        Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(Guid patientId, UpdatePatientDto dto, string updatedByUserId);

        /// <summary>Generate a unique HMS patient ID (HMS-YYYY-NNNNNN).</summary>
        Task<string> GenerateHmsPatientIdAsync();

        // ── Access Control ─────────────────────────────────────────
        Task<ApiResponse<string>> LockPatientRecordAsync(Guid patientId, LockRecordDto dto);
        Task<ApiResponse<string>> UnlockPatientRecordAsync(Guid patientId, UnlockRecordDto dto, string staffUserId, Guid hospitalId);
        Task<ApiResponse<string>> GrantEmergencyAccessAsync(Guid patientId, string grantedByAdminId, Guid hospitalId);

        // ── Emergency / Temporary ──────────────────────────────────
        Task<ApiResponse<TempRecordDto>> CreateTemporaryRecordAsync(CreateTempRecordDto dto, Guid hospitalId, string receptionistId);
        Task<ApiResponse<string>> MergeTemporaryRecordAsync(Guid tempRecordId, Guid realPatientId, string mergedByUserId);
    }

    // ── Medical Record ────────────────────────────────────────────────
    public interface IMedicalRecordService
    {
        Task<ApiResponse<MedicalRecordDto>> GetMedicalRecordAsync(Guid patientId, string requestingUserId, string requestingUserRole);

        // Vitals — Receptionist/Nurse
        Task<ApiResponse<VitalInfoDto>> UpdateVitalsAsync(Guid patientId, Guid visitId, UpdateVitalsDto dto, string updatedByUserId, string updatedByName);

        // Doctor notes — Doctor only
        Task<ApiResponse<DoctorNoteDto>> AddDoctorNoteAsync(Guid patientId, Guid visitId, AddDoctorNoteDto dto, string doctorUserId, string doctorName, Guid hospitalId, string hospitalName);
        Task<ApiResponse<DoctorNoteDto>> UpdateDoctorNoteAsync(Guid noteId, UpdateDoctorNoteDto dto, string doctorUserId);

        // Allergy info — Receptionist or Doctor
        Task<ApiResponse<AllergyInfoDto>> UpdateAllergyInfoAsync(Guid patientId, UpdateAllergyInfoDto dto, string updatedByUserId);

        // Prescriptions — Doctor only
        Task<ApiResponse<PrescriptionDto>> AddPrescriptionAsync(Guid patientId, Guid visitId, AddPrescriptionDto dto, string doctorUserId, string doctorName, Guid hospitalId);
    }

    // ── Lab ───────────────────────────────────────────────────────────
    public interface ILabService
    {
        // Doctor requests a lab test
        Task<ApiResponse<LabRequestDto>> CreateLabRequestAsync(Guid patientId, Guid visitId, CreateLabRequestDto dto, string doctorId, string doctorName, Guid hospitalId);

        // Lab technician updates results
        Task<ApiResponse<LabRequestDto>> UpdateLabResultAsync(Guid labRequestId, UpdateLabResultDto dto, string labTechId, string labTechName);
        Task<ApiResponse<LabTestResultDto>> AddTestResultAsync(Guid labRequestId, AddTestResultDto dto, string labTechId);

        // Queries
        Task<ApiResponse<List<LabRequestDto>>> GetLabRequestsByVisitAsync(Guid visitId);
        Task<ApiResponse<List<LabRequestDto>>> GetPendingLabRequestsAsync(Guid hospitalId);

        // Catalogue — Admin manages
        Task<ApiResponse<List<LabCatalogueItemDto>>> GetLabCatalogueAsync(Guid hospitalId);
        Task<ApiResponse<LabCatalogueItemDto>> AddLabCatalogueItemAsync(Guid hospitalId, AddLabCatalogueItemDto dto);
    }

    // ── Visit ─────────────────────────────────────────────────────────
    public interface IVisitService
    {
        // Check-in
        Task<ApiResponse<VisitDto>> CheckInPatientAsync(CheckInDto dto, string receptionistId, string receptionistName, Guid hospitalId);

        // Doctor assignment
        Task<ApiResponse<VisitDto>> AssignDoctorAsync(Guid visitId, AssignDoctorDto dto, string receptionistId);
        Task<ApiResponse<VisitDto>> DoctorGrantEntryAsync(Guid visitId, string doctorUserId);
        Task<ApiResponse<VisitDto>> StartConsultationAsync(Guid visitId, string doctorUserId);
        Task<ApiResponse<VisitDto>> EndConsultationAsync(Guid visitId, string doctorUserId);

        // Checkout
        Task<ApiResponse<VisitDto>> CheckOutPatientAsync(Guid visitId, CheckOutDto dto, string receptionistId);

        // Queries
        Task<ApiResponse<VisitDto>> GetVisitByIdAsync(Guid visitId);
        Task<ApiResponse<List<VisitSummaryDto>>> GetTodaysVisitsAsync(Guid hospitalId);
        Task<ApiResponse<List<VisitSummaryDto>>> GetPatientVisitHistoryAsync(Guid patientId);
    }

    // ── Appointment ───────────────────────────────────────────────────
    public interface IAppointmentService
    {
        // Patient requests
        Task<ApiResponse<AppointmentDto>> RequestAppointmentAsync(RequestAppointmentDto dto, Guid patientId);

        // Receptionist reviews
        Task<ApiResponse<AppointmentDto>> ReceptionistReviewAsync(Guid appointmentId, ReceptionistReviewDto dto, string receptionistId);

        // Doctor approves/rejects
        Task<ApiResponse<AppointmentDto>> DoctorRespondAsync(Guid appointmentId, DoctorRespondDto dto, string doctorUserId);

        // Queries
        Task<ApiResponse<List<AppointmentDto>>> GetHospitalAppointmentsAsync(Guid hospitalId, AppointmentStatus? status);
        Task<ApiResponse<List<AppointmentDto>>> GetDoctorAppointmentsAsync(string doctorUserId);
        Task<ApiResponse<List<AppointmentDto>>> GetPatientAppointmentsAsync(Guid patientId);
        Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(Guid appointmentId);
        Task<ApiResponse<string>> CancelAppointmentAsync(Guid appointmentId, string cancelledByUserId, string reason);
    }

    // ── Payment ───────────────────────────────────────────────────────
    public interface IPaymentService
    {
        Task<ApiResponse<PaymentDto>> CreatePaymentAsync(Guid visitId, CreatePaymentDto dto, string receptionistId, string receptionistName);
        Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(Guid paymentId, ProcessPaymentDto dto, string receptionistId);
        Task<ApiResponse<PaymentDto>> GetPaymentByVisitAsync(Guid visitId);
        Task<ApiResponse<PaymentDto>> WaivePaymentAsync(Guid paymentId, string reason, string adminUserId);
    }

    // ── Notification ──────────────────────────────────────────────────
    public interface INotificationService
    {
        // Send real-time + persist
        Task SendNotificationAsync(string recipientUserId, Shared.DTOs.Notification.NotificationType type, string title, string message, Guid hospitalId, object? payload = null, Guid? relatedVisitId = null, Guid? relatedAppointmentId = null, Guid? relatedPatientId = null);

        // Queries
        Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        Task<ApiResponse<int>> GetUnreadCountAsync(string userId);

        // Mark
        Task MarkAsReadAsync(Guid notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }

    // ── Department ────────────────────────────────────────────────────
    public interface IDepartmentService
    {
        Task<ApiResponse<List<DepartmentDto>>> GetDepartmentsAsync(Guid hospitalId);
        Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(Guid hospitalId, string name, string? description);
        Task<ApiResponse<List<DoctorSummaryDto>>> GetDoctorsByDepartmentAsync(Guid departmentId);
        Task<ApiResponse<List<DoctorSummaryDto>>> GetAvailableDoctorsAsync(Guid hospitalId);
    }

    // ── Audit ─────────────────────────────────────────────────────────
    public interface IAuditService
    {
        Task LogAsync(string userId, string userName, string userRole, string action, string entityType, string? entityId = null, object? oldValues = null, object? newValues = null, Guid? hospitalId = null, string? ipAddress = null);
        Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync(Guid hospitalId, DateTime? from, DateTime? to, string? entityType);
    }
}

// Bring enum into scope for IAppointmentService
namespace HMS.Service.Contracts
{
    using HMS.Entities.Enums;
}

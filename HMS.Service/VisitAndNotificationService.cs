using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Visit;
using HMS.Shared.DTOs.Notification;
using HMS.Service.Hubs;
using HMS.Shared.DTOs.Medical; // Added for AuditLogDto
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace HMS.Service
{
    // ── Visit Service ─────────────────────────────────────────────────
    public class VisitService : IVisitService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public VisitService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<ApiResponse<VisitDto>> CheckInPatientAsync(
            CheckInDto dto, string receptionistId, string receptionistName, Guid hospitalId)
        {
            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                .FirstOrDefaultAsync(p => p.Id == dto.PatientId);

            if (patient == null)
                return ApiResponse<VisitDto>.Failure("Patient not found.");

            if (patient.MedicalRecord == null)
                return ApiResponse<VisitDto>.Failure("Patient has no medical record.");

            var hospital = await _context.Hospitals.FindAsync(hospitalId);

            var visit = new HospitalVisit
            {
                Id = Guid.NewGuid(),
                PatientId = dto.PatientId,
                HospitalId = hospitalId,
                MedicalRecordId = patient.MedicalRecord.Id,
                VisitType = dto.VisitType,
                Status = VisitStatus.CheckedIn,
                AppointmentId = dto.AppointmentId,
                CheckInTime = DateTime.UtcNow,
                ChiefComplaintOnArrival = dto.ChiefComplaintOnArrival,
                IsEmergency = dto.IsEmergency,
                CheckedInByReceptionistId = receptionistId,
                CheckedInByReceptionistName = receptionistName,
            };

            _context.HospitalVisits.Add(visit);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(receptionistId, receptionistName,
                "Receptionist", "PATIENT_CHECKIN", "HospitalVisit",
                visit.Id.ToString(), hospitalId: hospitalId);

            return ApiResponse<VisitDto>.Success(
                MapToVisitDto(visit, patient, hospital),
                "Patient checked in successfully.");
        }

        public async Task<ApiResponse<VisitDto>> AssignDoctorAsync(
            Guid visitId, AssignDoctorDto dto, string receptionistId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            if (visit == null)
                return ApiResponse<VisitDto>.Failure("Visit not found.");

            visit.AssignedDoctorId = dto.DoctorUserId;
            visit.AssignedDoctorName = dto.DoctorName;
            visit.DoctorAssignedAt = DateTime.UtcNow;
            visit.Status = VisitStatus.DoctorNotified;
            visit.DoctorNotifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ── Real-time notification to doctor ──────────────────────
            var payload = new
            {
                visitId = visit.Id,
                patientId = visit.PatientId,
                patientName = visit.Patient?.FullName,
                hmsPatientId = visit.Patient?.HmsPatientId,
                chiefComplaint = visit.ChiefComplaintOnArrival,
                isEmergency = visit.IsEmergency,
                hospitalName = visit.Hospital?.Name,
                checkInTime = visit.CheckInTime
            };

            // Cast HMS.Entities.Enums to HMS.Shared.DTOs.Notification type
            var notifType = (HMS.Shared.DTOs.Notification.NotificationType)HMS.Entities.Enums.NotificationType.PatientAssigned;

            await _notificationService.SendNotificationAsync(
                recipientUserId: dto.DoctorUserId,
                type: notifType,
                title: visit.IsEmergency ? "🚨 EMERGENCY Patient Assigned" : "New Patient Assigned",
                message: $"{visit.Patient?.FullName} has checked in. Chief complaint: {visit.ChiefComplaintOnArrival ?? "Not specified"}",
                hospitalId: visit.HospitalId,
                payload: payload,
                relatedVisitId: visit.Id,
                relatedPatientId: visit.PatientId);

            return ApiResponse<VisitDto>.Success(
                MapToVisitDto(visit, visit.Patient, visit.Hospital),
                $"Doctor {dto.DoctorName} assigned and notified.");
        }

        public async Task<ApiResponse<VisitDto>> DoctorGrantEntryAsync(Guid visitId, string doctorUserId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId && v.AssignedDoctorId == doctorUserId);

            if (visit == null)
                return ApiResponse<VisitDto>.Failure("Visit not found or not assigned to you.");

            visit.Status = VisitStatus.DoctorGranted;
            visit.DoctorGrantedEntryAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<VisitDto>.Success(MapToVisitDto(visit, visit.Patient, visit.Hospital), "Entry granted.");
        }

        public async Task<ApiResponse<VisitDto>> StartConsultationAsync(Guid visitId, string doctorUserId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId && v.AssignedDoctorId == doctorUserId);

            if (visit == null) return ApiResponse<VisitDto>.Failure("Visit not found.");

            visit.Status = VisitStatus.InConsultation;
            visit.ConsultationStartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<VisitDto>.Success(MapToVisitDto(visit, visit.Patient, visit.Hospital));
        }

        public async Task<ApiResponse<VisitDto>> EndConsultationAsync(Guid visitId, string doctorUserId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId && v.AssignedDoctorId == doctorUserId);

            if (visit == null) return ApiResponse<VisitDto>.Failure("Visit not found.");

            visit.ConsultationEndedAt = DateTime.UtcNow;
            // Status update depends on your business flow (e.g., status = ConsultationCompleted)
            await _context.SaveChangesAsync();

            return ApiResponse<VisitDto>.Success(MapToVisitDto(visit, visit.Patient, visit.Hospital), "Consultation ended.");
        }

        public async Task<ApiResponse<VisitDto>> CheckOutPatientAsync(Guid visitId, CheckOutDto dto, string receptionistId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            if (visit == null) return ApiResponse<VisitDto>.Failure("Visit not found.");

            visit.Status = VisitStatus.CheckedOut;
            visit.CheckOutTime = DateTime.UtcNow;
            visit.CheckedOutByReceptionistId = receptionistId;
            visit.DischargeNotes = dto.DischargeNotes;
            visit.FollowUpDate = dto.FollowUpDate;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(receptionistId, "", "Receptionist", "PATIENT_CHECKOUT", "HospitalVisit", visit.Id.ToString(), hospitalId: visit.HospitalId);

            return ApiResponse<VisitDto>.Success(MapToVisitDto(visit, visit.Patient, visit.Hospital), "Checked out.");
        }

        public async Task<ApiResponse<VisitDto>> GetVisitByIdAsync(Guid visitId)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Include(v => v.Hospital)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            return visit == null
                ? ApiResponse<VisitDto>.Failure("Not found")
                : ApiResponse<VisitDto>.Success(MapToVisitDto(visit, visit.Patient, visit.Hospital));
        }

        public async Task<ApiResponse<List<VisitSummaryDto>>> GetTodaysVisitsAsync(Guid hospitalId)
        {
            var today = DateTime.UtcNow.Date;
            var visits = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Where(v => v.HospitalId == hospitalId && v.CheckInTime.Date == today)
                .OrderByDescending(v => v.CheckInTime)
                .Select(v => new VisitSummaryDto
                {
                    Id = v.Id,
                    PatientName = v.Patient!.FullName,
                    HmsPatientId = v.Patient.HmsPatientId,
                    Status = v.Status.ToString(),
                    VisitType = v.VisitType.ToString(),
                    CheckInTime = v.CheckInTime,
                    IsEmergency = v.IsEmergency,
                    AssignedDoctorName = v.AssignedDoctorName,
                    ChiefComplaint = v.ChiefComplaintOnArrival
                }).ToListAsync();

            return ApiResponse<List<VisitSummaryDto>>.Success(visits);
        }

        public async Task<ApiResponse<List<VisitSummaryDto>>> GetPatientVisitHistoryAsync(Guid patientId)
        {
            var visits = await _context.HospitalVisits
                .Include(v => v.Patient)
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.CheckInTime)
                .Select(v => new VisitSummaryDto
                {
                    Id = v.Id,
                    PatientName = v.Patient!.FullName,
                    HmsPatientId = v.Patient.HmsPatientId,
                    Status = v.Status.ToString(),
                    VisitType = v.VisitType.ToString(),
                    CheckInTime = v.CheckInTime,
                    IsEmergency = v.IsEmergency,
                    AssignedDoctorName = v.AssignedDoctorName,
                    ChiefComplaint = v.ChiefComplaintOnArrival
                }).ToListAsync();

            return ApiResponse<List<VisitSummaryDto>>.Success(visits);
        }

        private static VisitDto MapToVisitDto(HospitalVisit v, Patient? p, Hospital? h) => new()
        {
            Id = v.Id,
            PatientId = v.PatientId,
            PatientName = p?.FullName ?? string.Empty,
            HmsPatientId = p?.HmsPatientId ?? string.Empty,
            HospitalId = v.HospitalId,
            HospitalName = h?.Name ?? string.Empty,
            VisitType = v.VisitType.ToString(),
            Status = v.Status.ToString(),
            CheckInTime = v.CheckInTime,
            CheckOutTime = v.CheckOutTime,
            ChiefComplaintOnArrival = v.ChiefComplaintOnArrival,
            IsEmergency = v.IsEmergency,
            AssignedDoctorName = v.AssignedDoctorName,
            AssignedDoctorId = v.AssignedDoctorId,
            DoctorAssignedAt = v.DoctorAssignedAt,
            DoctorGrantedEntryAt = v.DoctorGrantedEntryAt,
            ConsultationStartedAt = v.ConsultationStartedAt,
            ConsultationEndedAt = v.ConsultationEndedAt,
            DischargeNotes = v.DischargeNotes,
            FollowUpDate = v.FollowUpDate,
            LabReferralMade = v.LabReferralMade,
            WasTemporaryRecord = v.WasTemporaryRecord,
        };
    }

    // ── Audit Service ─────────────────────────────────────────────────
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        public AuditService(ApplicationDbContext context) => _context = context;

        public async Task LogAsync(string userId, string userName, string userRole, string action, string entityType, string? entityId = null, object? oldValues = null, object? newValues = null, Guid? hospitalId = null, string? ipAddress = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                UserRole = userRole,
                HospitalId = hospitalId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<ApiResponse<List<AuditLogDto>>> GetAuditLogsAsync(Guid hospitalId, DateTime? from, DateTime? to, string? entityType)
        {
            var query = _context.AuditLogs.Where(a => a.HospitalId == hospitalId);

            if (from != null) query = query.Where(a => a.Timestamp >= from);
            if (to != null) query = query.Where(a => a.Timestamp <= to);
            if (entityType != null) query = query.Where(a => a.EntityType == entityType);

            var logs = await query.OrderByDescending(a => a.Timestamp).Take(500)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserName = a.UserName,
                    UserRole = a.UserRole,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    Timestamp = a.Timestamp
                }).ToListAsync();

            return ApiResponse<List<AuditLogDto>>.Success(logs);
        }
    }

    // ── Notification Service ──────────────────────────────────────────
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hub)
        {
            _context = context;
            _hub = hub;
        }

        public async Task SendNotificationAsync(string recipientUserId, HMS.Shared.DTOs.Notification.NotificationType type, string title, string message, Guid hospitalId, object? payload = null, Guid? relatedVisitId = null, Guid? relatedAppointmentId = null, Guid? relatedPatientId = null)
        {
            var notification = new HMS.Entities.Models.Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientUserId,
                HospitalId = hospitalId,
                // Internal database might use the domain enum
                Type = (HMS.Entities.Enums.NotificationType)type,
                Status = HMS.Entities.Enums.NotificationStatus.Unread,
                Title = title,
                Message = message,
                Payload = payload != null ? JsonSerializer.Serialize(payload) : null,
                RelatedVisitId = relatedVisitId,
                RelatedAppointmentId = relatedAppointmentId,
                RelatedPatientId = relatedPatientId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            try
            {
                await _hub.Clients.Group(recipientUserId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    type = type.ToString(),
                    title,
                    message,
                    payload,
                    relatedVisitId,
                    createdAt = notification.CreatedAt
                });

                notification.WasDeliveredRealTime = true;
                await _context.SaveChangesAsync();
            }
            catch { /* Offline handling */ }
        }

        public async Task<ApiResponse<List<NotificationDto>>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            var query = _context.Notifications.Where(n => n.RecipientUserId == userId && (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow));
            if (unreadOnly) query = query.Where(n => n.Status == NotificationStatus.Unread);

            var notifications = await query.OrderByDescending(n => n.CreatedAt).Take(50)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type.ToString(),
                    Status = n.Status.ToString(),
                    Title = n.Title,
                    Message = n.Message,
                    Payload = n.Payload,
                    RelatedPatientId = n.RelatedPatientId,
                    RelatedVisitId = n.RelatedVisitId,
                    RelatedAppointmentId = n.RelatedAppointmentId,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    WasDeliveredRealTime = n.WasDeliveredRealTime
                }).ToListAsync();

            return ApiResponse<List<NotificationDto>>.Success(notifications);
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(string userId)
        {
            var count = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && n.Status == NotificationStatus.Unread);
            return ApiResponse<int>.Success(count);
        }

        public async Task MarkAsReadAsync(Guid notificationId, string userId)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == userId);
            if (n == null) return;
            n.Status = NotificationStatus.Read;
            n.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var list = await _context.Notifications.Where(n => n.RecipientUserId == userId && n.Status == NotificationStatus.Unread).ToListAsync();
            foreach (var n in list) { n.Status = NotificationStatus.Read; n.ReadAt = DateTime.UtcNow; }
            await _context.SaveChangesAsync();
        }
    }
}
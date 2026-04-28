using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Appointment;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public AppointmentService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        // ── Patient requests appointment ──────────────────────────────
        public async Task<ApiResponse<AppointmentDto>> RequestAppointmentAsync(
            RequestAppointmentDto dto, Guid patientId)
        {
            var hospital = await _context.Hospitals.FindAsync(dto.HospitalId);
            if (hospital == null)
                return ApiResponse<AppointmentDto>.Failure("Hospital not found.");

            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return ApiResponse<AppointmentDto>.Failure("Patient not found.");

            var existing = await _context.Appointments.AnyAsync(a =>
                a.PatientId == patientId &&
                a.HospitalId == dto.HospitalId &&
                a.Status == AppointmentStatus.Pending);

            if (existing)
                return ApiResponse<AppointmentDto>.Failure(
                    "You already have a pending appointment at this hospital.");

            var appointment = new Appointment
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                HospitalId = dto.HospitalId,
                PreferredDoctorId = dto.PreferredDoctorId,
                RequestedDateTime = dto.RequestedDateTime,
                Type = dto.Type,
                Status = AppointmentStatus.Pending,
                ReasonForAppointment = dto.ReasonForAppointment,
                BookedViaApp = true,
                CreatedAt = DateTime.UtcNow,
            };

            if (!string.IsNullOrEmpty(dto.PreferredDoctorId))
            {
                var doctor = await _context.Users.FindAsync(dto.PreferredDoctorId);
                appointment.PreferredDoctorName = doctor?.FullName;
            }

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var receptionists = await _context.Users
                .Where(u => u.HospitalId == dto.HospitalId &&
                            u.Role == UserRole.Receptionist && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var recId in receptionists)
            {
                await _notificationService.SendNotificationAsync(
                    recipientUserId: recId,
                    // FIX: Explicitly use Shared DTO Enum
                    type: HMS.Shared.DTOs.Notification.NotificationType.AppointmentRequest,
                    title: "New Appointment Request",
                    message: $"{patient.FullName} requests an appointment on {dto.RequestedDateTime:MMM dd, yyyy HH:mm}",
                    hospitalId: dto.HospitalId,
                    relatedAppointmentId: appointment.Id,
                    relatedPatientId: patientId);
            }

            return ApiResponse<AppointmentDto>.Success(
                await MapToDto(appointment), "Appointment request sent.");
        }

        // ── Receptionist reviews ──────────────────────────────────────
        public async Task<ApiResponse<AppointmentDto>> ReceptionistReviewAsync(
            Guid appointmentId, ReceptionistReviewDto dto, string receptionistId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return ApiResponse<AppointmentDto>.Failure("Appointment not found.");

            if (appointment.Status != AppointmentStatus.Pending)
                return ApiResponse<AppointmentDto>.Failure(
                    "This appointment has already been reviewed.");

            appointment.ReviewedByReceptionistId = receptionistId;
            appointment.ReviewedByReceptionistAt = DateTime.UtcNow;
            appointment.ReceptionistApproved = dto.Approved;
            appointment.ReceptionistNote = dto.Note;
            appointment.LastUpdated = DateTime.UtcNow;

            if (!dto.Approved)
            {
                appointment.Status = AppointmentStatus.Rejected;
                appointment.RejectedBy = receptionistId;
                appointment.RejectionReason = dto.Note;

                if (appointment.Patient?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientUserId: appointment.Patient.UserId,
                        // FIX: Explicitly use Shared DTO Enum
                        type: HMS.Shared.DTOs.Notification.NotificationType.AppointmentRejected,
                        title: "Appointment Request Declined",
                        message: $"Your appointment at {appointment.Hospital?.Name} was declined. Reason: {dto.Note}",
                        hospitalId: appointment.HospitalId,
                        relatedAppointmentId: appointmentId);
                }
            }
            else
            {
                appointment.Status = AppointmentStatus.AwaitingDoctor;

                if (!string.IsNullOrEmpty(dto.AssignedDoctorId))
                {
                    appointment.AssignedDoctorId = dto.AssignedDoctorId;
                    appointment.AssignedDoctorName = dto.AssignedDoctorName;

                    await _notificationService.SendNotificationAsync(
                        recipientUserId: dto.AssignedDoctorId,
                        // FIX: Explicitly use Shared DTO Enum
                        type: HMS.Shared.DTOs.Notification.NotificationType.AppointmentRequest,
                        title: "Appointment Requires Your Approval",
                        message: $"{appointment.Patient?.FullName} requested an appointment on {appointment.RequestedDateTime:MMM dd, yyyy HH:mm}. Please confirm.",
                        hospitalId: appointment.HospitalId,
                        relatedAppointmentId: appointmentId,
                        relatedPatientId: appointment.PatientId);
                }
            }

            await _context.SaveChangesAsync();

            return ApiResponse<AppointmentDto>.Success(
                await MapToDto(appointment),
                dto.Approved ? "Appointment approved. Waiting for doctor confirmation." : "Appointment rejected.");
        }

        // ── Doctor responds ───────────────────────────────────────────
        public async Task<ApiResponse<AppointmentDto>> DoctorRespondAsync(
            Guid appointmentId, DoctorRespondDto dto, string doctorUserId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .FirstOrDefaultAsync(a => a.Id == appointmentId &&
                                          a.AssignedDoctorId == doctorUserId);

            if (appointment == null)
                return ApiResponse<AppointmentDto>.Failure(
                    "Appointment not found or not assigned to you.");

            appointment.DoctorRespondedAt = DateTime.UtcNow;
            appointment.DoctorApproved = dto.Approved;
            appointment.DoctorNote = dto.Note;
            appointment.LastUpdated = DateTime.UtcNow;

            if (!dto.Approved)
            {
                appointment.Status = AppointmentStatus.Rejected;
                appointment.RejectedBy = doctorUserId;
                appointment.RejectionReason = dto.RejectionReason ?? dto.Note;

                if (appointment.Patient?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientUserId: appointment.Patient.UserId,
                        // FIX: Explicitly use Shared DTO Enum
                        type: HMS.Shared.DTOs.Notification.NotificationType.AppointmentRejected,
                        title: "Appointment Not Available",
                        message: $"Dr. {appointment.AssignedDoctorName} cannot take your appointment. Reason: {dto.RejectionReason}",
                        hospitalId: appointment.HospitalId,
                        relatedAppointmentId: appointmentId);
                }
            }
            else
            {
                appointment.Status = AppointmentStatus.Confirmed;
                appointment.ConfirmedDateTime = dto.ConfirmedDateTime ?? appointment.RequestedDateTime;

                if (appointment.Patient?.UserId != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientUserId: appointment.Patient.UserId,
                        // FIX: Explicitly use Shared DTO Enum
                        type: HMS.Shared.DTOs.Notification.NotificationType.AppointmentConfirmed,
                        title: "Appointment Confirmed ✓",
                        message: $"Your appointment with Dr. {appointment.AssignedDoctorName} at {appointment.Hospital?.Name} is confirmed for {appointment.ConfirmedDateTime:MMM dd, yyyy HH:mm}",
                        hospitalId: appointment.HospitalId,
                        relatedAppointmentId: appointmentId);
                }
            }

            await _context.SaveChangesAsync();

            return ApiResponse<AppointmentDto>.Success(
                await MapToDto(appointment),
                dto.Approved ? "Appointment confirmed. Patient notified." : "Appointment rejected.");
        }

        // ── Cancel ────────────────────────────────────────────────────
        public async Task<ApiResponse<string>> CancelAppointmentAsync(
            Guid appointmentId, string cancelledByUserId, string reason)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return ApiResponse<string>.Failure("Appointment not found.");

            if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
                return ApiResponse<string>.Failure("Appointment cannot be cancelled.");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.RejectedBy = cancelledByUserId;
            appointment.RejectionReason = reason;
            appointment.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ApiResponse<string>.Success("", "Appointment cancelled.");
        }

        // ── Queries ───────────────────────────────────────────────────
        public async Task<ApiResponse<List<AppointmentDto>>> GetHospitalAppointmentsAsync(
            Guid hospitalId, AppointmentStatus? status)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .Where(a => a.HospitalId == hospitalId);

            if (status != null) query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderByDescending(a => a.RequestedDateTime)
                .ToListAsync();

            var dtos = new List<AppointmentDto>();
            foreach (var a in appointments) dtos.Add(await MapToDto(a));

            return ApiResponse<List<AppointmentDto>>.Success(dtos);
        }

        public async Task<ApiResponse<List<AppointmentDto>>> GetDoctorAppointmentsAsync(
            string doctorUserId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .Where(a => a.AssignedDoctorId == doctorUserId &&
                            a.Status == AppointmentStatus.Confirmed)
                .OrderBy(a => a.ConfirmedDateTime)
                .ToListAsync();

            var dtos = new List<AppointmentDto>();
            foreach (var a in appointments) dtos.Add(await MapToDto(a));

            return ApiResponse<List<AppointmentDto>>.Success(dtos);
        }

        public async Task<ApiResponse<List<AppointmentDto>>> GetPatientAppointmentsAsync(
            Guid patientId)
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var dtos = new List<AppointmentDto>();
            foreach (var a in appointments) dtos.Add(await MapToDto(a));

            return ApiResponse<List<AppointmentDto>>.Success(dtos);
        }

        public async Task<ApiResponse<AppointmentDto>> GetAppointmentByIdAsync(Guid appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return ApiResponse<AppointmentDto>.Failure("Appointment not found.");

            return ApiResponse<AppointmentDto>.Success(await MapToDto(appointment));
        }

        // ── Helper ────────────────────────────────────────────────────
        private Task<AppointmentDto> MapToDto(Appointment a) =>
            Task.FromResult(new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient?.FullName ?? string.Empty,
                HmsPatientId = a.Patient?.HmsPatientId ?? string.Empty,
                HospitalId = a.HospitalId,
                HospitalName = a.Hospital?.Name ?? string.Empty,
                PreferredDoctorName = a.PreferredDoctorName,
                AssignedDoctorName = a.AssignedDoctorName,
                RequestedDateTime = a.RequestedDateTime,
                ConfirmedDateTime = a.ConfirmedDateTime,
                Type = a.Type.ToString(),
                Status = a.Status.ToString(),
                ReasonForAppointment = a.ReasonForAppointment,
                ReceptionistApproved = a.ReceptionistApproved,
                ReceptionistNote = a.ReceptionistNote,
                DoctorApproved = a.DoctorApproved,
                DoctorNote = a.DoctorNote,
                RejectionReason = a.RejectionReason,
                BookedViaApp = a.BookedViaApp,
                CreatedAt = a.CreatedAt,
            });
    }
}
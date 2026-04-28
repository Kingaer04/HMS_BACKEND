using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Lab;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class LabService : ILabService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public LabService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IAuditService auditService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        // ── Doctor creates lab request ────────────────────────────────
        public async Task<ApiResponse<LabRequestDto>> CreateLabRequestAsync(
            Guid patientId, Guid visitId,
            CreateLabRequestDto dto,
            string doctorId, string doctorName, Guid hospitalId)
        {
            var record = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<LabRequestDto>.Failure("Medical record not found.");

            var visit = await _context.HospitalVisits
                .FirstOrDefaultAsync(v => v.Id == visitId &&
                                          v.AssignedDoctorId == doctorId);
            if (visit == null)
                return ApiResponse<LabRequestDto>.Failure(
                    "You are not the assigned doctor for this visit.");

            var labRequest = new LabRequest
            {
                Id = Guid.NewGuid(),
                MedicalRecordId = record.Id,
                VisitId = visitId,
                HospitalId = hospitalId,
                RequestedByDoctorId = doctorId,
                RequestedByDoctorName = doctorName,
                ClinicalNotes = dto.ClinicalNotes,
                Status = LabTestStatus.Requested,
                RequestedAt = DateTime.UtcNow,
                TestResults = dto.TestNames.Select(name => new LabTestResult
                {
                    Id = Guid.NewGuid(),
                    TestName = name,
                    TestCode = name.ToUpper().Replace(" ", "_"),
                }).ToList()
            };

            _context.LabRequests.Add(labRequest);

            visit.LabReferralMade = true;
            visit.LabReferralAt = DateTime.UtcNow;
            visit.Status = VisitStatus.LabReferred;

            await _context.SaveChangesAsync();

            var labTechs = await _context.Users
                .Where(u => u.HospitalId == hospitalId &&
                            u.Role == UserRole.LabTechnician && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var techId in labTechs)
            {
                await _notificationService.SendNotificationAsync(
                    recipientUserId: techId,
                    // FIX: Explicitly use the Shared DTO NotificationType to match the Interface
                    type: HMS.Shared.DTOs.Notification.NotificationType.LabResultReady,
                    title: "New Lab Request",
                    message: $"Lab tests requested by Dr. {doctorName}: {string.Join(", ", dto.TestNames)}",
                    hospitalId: hospitalId,
                    relatedVisitId: visitId,
                    relatedPatientId: patientId);
            }

            await _auditService.LogAsync(doctorId, doctorName, "Doctor",
                "CREATE_LAB_REQUEST", "LabRequest", labRequest.Id.ToString(),
                hospitalId: hospitalId);

            return ApiResponse<LabRequestDto>.Success(
                MapToDto(labRequest), "Lab request created. Lab technicians notified.");
        }

        // ── Lab tech updates request status ──────────────────────────
        public async Task<ApiResponse<LabRequestDto>> UpdateLabResultAsync(
            Guid labRequestId, UpdateLabResultDto dto,
            string labTechId, string labTechName)
        {
            var request = await _context.LabRequests
                .Include(r => r.TestResults)
                .FirstOrDefaultAsync(r => r.Id == labRequestId);

            if (request == null)
                return ApiResponse<LabRequestDto>.Failure("Lab request not found.");

            request.Status = dto.Status;
            request.ProcessedByLabTechId = labTechId;
            request.ProcessedByLabTechName = labTechName;

            if (dto.SampleCollectedAt != null)
                request.SampleCollectedAt = dto.SampleCollectedAt;

            if (dto.Status == LabTestStatus.Completed)
            {
                request.ResultReadyAt = DateTime.UtcNow;

                await _notificationService.SendNotificationAsync(
                    recipientUserId: request.RequestedByDoctorId,
                    // FIX: Explicitly use the Shared DTO NotificationType to match the Interface
                    type: HMS.Shared.DTOs.Notification.NotificationType.LabResultReady,
                    title: "Lab Results Ready",
                    message: $"Results for visit {request.VisitId} are ready.",
                    hospitalId: request.HospitalId,
                    relatedVisitId: request.VisitId,
                    relatedPatientId: null);
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(labTechId, labTechName, "LabTechnician",
                "UPDATE_LAB_STATUS", "LabRequest", request.Id.ToString(),
                hospitalId: request.HospitalId);

            return ApiResponse<LabRequestDto>.Success(MapToDto(request));
        }

        public async Task<ApiResponse<LabTestResultDto>> AddTestResultAsync(
            Guid labRequestId, AddTestResultDto dto, string labTechId)
        {
            var request = await _context.LabRequests
                .Include(r => r.TestResults)
                .FirstOrDefaultAsync(r => r.Id == labRequestId);

            if (request == null)
                return ApiResponse<LabTestResultDto>.Failure("Lab request not found.");

            var existing = request.TestResults
                .FirstOrDefault(t => t.TestCode == dto.TestCode);

            if (existing != null)
            {
                existing.Result = dto.Result;
                existing.Unit = dto.Unit;
                existing.ReferenceRange = dto.ReferenceRange;
                existing.IsAbnormal = dto.IsAbnormal;
                existing.Notes = dto.Notes;
                existing.EnteredByLabTechId = labTechId;
                existing.EnteredAt = DateTime.UtcNow;
            }
            else
            {
                var result = new LabTestResult
                {
                    Id = Guid.NewGuid(),
                    LabRequestId = labRequestId,
                    TestName = dto.TestName,
                    TestCode = dto.TestCode,
                    Result = dto.Result,
                    Unit = dto.Unit,
                    ReferenceRange = dto.ReferenceRange,
                    IsAbnormal = dto.IsAbnormal,
                    Notes = dto.Notes,
                    EnteredByLabTechId = labTechId,
                    EnteredAt = DateTime.UtcNow
                };
                _context.LabTestResults.Add(result);
                existing = result;
            }

            await _context.SaveChangesAsync();

            return ApiResponse<LabTestResultDto>.Success(new LabTestResultDto
            {
                Id = existing.Id,
                TestName = existing.TestName,
                TestCode = existing.TestCode,
                Result = existing.Result,
                Unit = existing.Unit,
                ReferenceRange = existing.ReferenceRange,
                IsAbnormal = existing.IsAbnormal,
                Notes = existing.Notes,
                EnteredAt = existing.EnteredAt,
            });
        }

        public async Task<ApiResponse<List<LabRequestDto>>> GetLabRequestsByVisitAsync(Guid visitId)
        {
            var requests = await _context.LabRequests
                .Include(r => r.TestResults)
                .Where(r => r.VisitId == visitId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return ApiResponse<List<LabRequestDto>>.Success(
                requests.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<LabRequestDto>>> GetPendingLabRequestsAsync(Guid hospitalId)
        {
            var requests = await _context.LabRequests
                .Include(r => r.TestResults)
                .Where(r => r.HospitalId == hospitalId &&
                            r.Status != LabTestStatus.Completed &&
                            r.Status != LabTestStatus.Cancelled)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();

            return ApiResponse<List<LabRequestDto>>.Success(
                requests.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<List<LabCatalogueItemDto>>> GetLabCatalogueAsync(Guid hospitalId)
        {
            var items = await _context.LabTestCatalogues
                .Where(c => c.HospitalId == hospitalId && c.IsActive)
                .OrderBy(c => c.Category).ThenBy(c => c.TestName)
                .Select(c => new LabCatalogueItemDto
                {
                    Id = c.Id,
                    TestName = c.TestName,
                    TestCode = c.TestCode,
                    Category = c.Category,
                    Price = c.Price,
                    ExpectedTurnaroundTime = c.ExpectedTurnaroundTime,
                    IsActive = c.IsActive,
                })
                .ToListAsync();

            return ApiResponse<List<LabCatalogueItemDto>>.Success(items);
        }

        public async Task<ApiResponse<LabCatalogueItemDto>> AddLabCatalogueItemAsync(
            Guid hospitalId, AddLabCatalogueItemDto dto)
        {
            var item = new LabTestCatalogue
            {
                Id = Guid.NewGuid(),
                HospitalId = hospitalId,
                TestName = dto.TestName,
                TestCode = dto.TestCode.ToUpper(),
                Category = dto.Category,
                Description = dto.Description,
                Price = dto.Price,
                ExpectedTurnaroundTime = dto.ExpectedTurnaroundTime,
                IsActive = true,
            };

            _context.LabTestCatalogues.Add(item);
            await _context.SaveChangesAsync();

            return ApiResponse<LabCatalogueItemDto>.Success(new LabCatalogueItemDto
            {
                Id = item.Id,
                TestName = item.TestName,
                TestCode = item.TestCode,
                Category = item.Category,
                Price = item.Price,
                ExpectedTurnaroundTime = item.ExpectedTurnaroundTime,
                IsActive = item.IsActive,
            }, "Test added to catalogue.");
        }

        private static LabRequestDto MapToDto(LabRequest r) => new()
        {
            Id = r.Id,
            RequestedByDoctorName = r.RequestedByDoctorName,
            ClinicalNotes = r.ClinicalNotes,
            Status = r.Status.ToString(),
            RequestedAt = r.RequestedAt,
            ResultReadyAt = r.ResultReadyAt,
            ProcessedByLabTechName = r.ProcessedByLabTechName,
            TestResults = r.TestResults.Select(t => new LabTestResultDto
            {
                Id = t.Id,
                TestName = t.TestName,
                TestCode = t.TestCode,
                Result = t.Result,
                Unit = t.Unit,
                ReferenceRange = t.ReferenceRange,
                IsAbnormal = t.IsAbnormal,
                Notes = t.Notes,
                EnteredAt = t.EnteredAt,
            }).ToList()
        };
    }
}
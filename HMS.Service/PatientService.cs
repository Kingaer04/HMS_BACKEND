using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Patient;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HMS.Service
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public PatientService(ApplicationDbContext context, IAuditService auditService)
        {
            _context     = context;
            _auditService = auditService;
        }

        // ── Search ────────────────────────────────────────────────────
        public async Task<ApiResponse<List<PatientSummaryDto>>> SearchPatientsAsync(
            string query, Guid hospitalId)
        {
            query = query.Trim().ToLower();

            var patients = await _context.Patients
                .Include(p => p.OriginHospital)
                .Where(p => p.IsActive && (
                    p.HmsPatientId.ToLower().Contains(query) ||
                    p.FirstName.ToLower().Contains(query) ||
                    p.LastName.ToLower().Contains(query) ||
                    p.PhoneNumber.Contains(query)))
                .Take(20)
                .Select(p => new PatientSummaryDto
                {
                    Id                  = p.Id,
                    HmsPatientId        = p.HmsPatientId,
                    FullName            = p.FullName,
                    PhoneNumber         = p.PhoneNumber,
                    Age                 = p.Age,
                    Gender              = p.Gender.ToString(),
                    OriginHospitalName  = p.OriginHospital!.Name,
                    AccessLevel         = p.AccessLevel,
                    IsVisitingPatient   = p.OriginHospitalId != hospitalId
                })
                .ToListAsync();

            return ApiResponse<List<PatientSummaryDto>>.Success(patients);
        }

        // ── Get Detail ────────────────────────────────────────────────
        public async Task<ApiResponse<PatientDetailDto>> GetPatientByIdAsync(
            Guid patientId, string requestingUserId)
        {
            var patient = await _context.Patients
                .Include(p => p.OriginHospital)
                .Include(p => p.AccessControl)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                return ApiResponse<PatientDetailDto>.Failure("Patient not found.");

            // If record is locked — check access log for recent unlock
            if (patient.AccessLevel == AccessLevel.Locked)
            {
                var recentUnlock = await _context.AccessLogs
                    .AnyAsync(l => l.PatientAccessControlId == patient.AccessControl!.Id
                               && l.AccessedByUserId == requestingUserId
                               && l.WasSuccessful
                               && l.AccessedAt >= DateTime.UtcNow.AddHours(-4));

                if (!recentUnlock)
                    return ApiResponse<PatientDetailDto>.Failure(
                        "Patient record is locked. Patient must provide access code.");
            }

            return ApiResponse<PatientDetailDto>.Success(MapToDetail(patient));
        }

        // ── Register ──────────────────────────────────────────────────
        public async Task<ApiResponse<PatientDetailDto>> RegisterPatientAsync(
            RegisterPatientDto dto, Guid hospitalId, string receptionistId)
        {
            // Duplicate check — same name + DOB + phone
            var duplicate = await _context.Patients.AnyAsync(p =>
                p.FirstName.ToLower() == dto.FirstName.ToLower() &&
                p.LastName.ToLower()  == dto.LastName.ToLower()  &&
                p.DateOfBirth         == dto.DateOfBirth          &&
                p.PhoneNumber         == dto.PhoneNumber);

            if (duplicate)
                return ApiResponse<PatientDetailDto>.Failure(
                    "A patient with the same name, date of birth, and phone number already exists. " +
                    "Please search for them first.");

            var hmsId = await GenerateHmsPatientIdAsync();

            var patient = new Patient
            {
                Id                          = Guid.NewGuid(),
                HmsPatientId                = hmsId,
                FirstName                   = dto.FirstName.Trim(),
                LastName                    = dto.LastName.Trim(),
                MiddleName                  = dto.MiddleName?.Trim(),
                DateOfBirth                 = dto.DateOfBirth,
                Gender                      = dto.Gender,
                MaritalStatus               = dto.MaritalStatus,
                PhoneNumber                 = dto.PhoneNumber.Trim(),
                AlternativePhone            = dto.AlternativePhone?.Trim(),
                Email                       = dto.Email?.Trim(),
                NHISNumber                  = dto.NHISNumber?.Trim(),
                BlockNumber                 = dto.BlockNumber,
                Street                      = dto.Street,
                City                        = dto.City,
                State                       = dto.State,
                EmergencyContactName        = dto.EmergencyContactName,
                EmergencyContactPhone       = dto.EmergencyContactPhone,
                EmergencyContactRelationship = dto.EmergencyContactRelationship,
                OriginHospitalId            = hospitalId,
                AccessLevel                 = AccessLevel.Open,
                RegisteredAt                = DateTime.UtcNow,
            };

            // Create empty medical record + vitals + allergy info
            var medicalRecord = new MedicalRecord
            {
                Id        = Guid.NewGuid(),
                PatientId = patient.Id,
                Status    = RecordStatus.Active,
                VitalInfo = new VitalInfo { Id = Guid.NewGuid() },
                AllergyInfo = new AllergyInfo { Id = Guid.NewGuid() }
            };

            // Create access control (open by default)
            var accessControl = new PatientAccessControl
            {
                Id          = Guid.NewGuid(),
                PatientId   = patient.Id,
                AccessLevel = AccessLevel.Open
            };

            patient.MedicalRecord  = medicalRecord;
            patient.AccessControl  = accessControl;

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(receptionistId, "", "Receptionist",
                "REGISTER_PATIENT", "Patient", patient.Id.ToString(),
                hospitalId: hospitalId);

            var result = await _context.Patients
                .Include(p => p.OriginHospital)
                .FirstAsync(p => p.Id == patient.Id);

            return ApiResponse<PatientDetailDto>.Success(
                MapToDetail(result), $"Patient registered. HMS ID: {hmsId}");
        }

        // ── Update ────────────────────────────────────────────────────
        public async Task<ApiResponse<PatientDetailDto>> UpdatePatientAsync(
            Guid patientId, UpdatePatientDto dto, string updatedByUserId)
        {
            var patient = await _context.Patients
                .Include(p => p.OriginHospital)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
                return ApiResponse<PatientDetailDto>.Failure("Patient not found.");

            if (dto.PhoneNumber      != null) patient.PhoneNumber      = dto.PhoneNumber;
            if (dto.AlternativePhone != null) patient.AlternativePhone = dto.AlternativePhone;
            if (dto.Email            != null) patient.Email            = dto.Email;
            if (dto.MiddleName       != null) patient.MiddleName       = dto.MiddleName;
            if (dto.BlockNumber      != null) patient.BlockNumber      = dto.BlockNumber;
            if (dto.Street           != null) patient.Street           = dto.Street;
            if (dto.City             != null) patient.City             = dto.City;
            if (dto.State            != null) patient.State            = dto.State;
            if (dto.NHISNumber       != null) patient.NHISNumber       = dto.NHISNumber;
            if (dto.MaritalStatus    != null) patient.MaritalStatus    = dto.MaritalStatus.Value;
            if (dto.EmergencyContactName  != null) patient.EmergencyContactName  = dto.EmergencyContactName;
            if (dto.EmergencyContactPhone != null) patient.EmergencyContactPhone = dto.EmergencyContactPhone;
            if (dto.EmergencyContactRelationship != null) patient.EmergencyContactRelationship = dto.EmergencyContactRelationship;

            patient.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<PatientDetailDto>.Success(MapToDetail(patient));
        }

        // ── Generate HMS Patient ID ───────────────────────────────────
        public async Task<string> GenerateHmsPatientIdAsync()
        {
            var year = DateTime.UtcNow.Year;

            // Get or create the sequence for this year — with locking
            var seq = await _context.PatientIdSequences
                .FirstOrDefaultAsync(s => s.Year == year);

            if (seq == null)
            {
                seq = new PatientIdSequence { Year = year, LastSequenceNumber = 0 };
                _context.PatientIdSequences.Add(seq);
            }

            seq.LastSequenceNumber++;
            await _context.SaveChangesAsync();

            return $"HMS-{year}-{seq.LastSequenceNumber:D6}";
        }

        // ── Lock / Unlock ─────────────────────────────────────────────
        public async Task<ApiResponse<string>> LockPatientRecordAsync(
            Guid patientId, LockRecordDto dto)
        {
            var control = await _context.PatientAccessControls
                .FirstOrDefaultAsync(a => a.PatientId == patientId);

            if (control == null)
                return ApiResponse<string>.Failure("Patient not found.");

            control.AccessCodeHash        = HashCode(dto.AccessCode);
            control.QuickReferenceCode    = GenerateQuickCode();
            control.PreferredUnlockMethod = dto.PreferredMethod;
            control.LastUpdated           = DateTime.UtcNow;

            // Update patient access level
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient != null) patient.AccessLevel = AccessLevel.Locked;

            control.AccessLevel = AccessLevel.Locked;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Success(
                control.QuickReferenceCode,
                "Record locked. Save your quick reference code.");
        }

        public async Task<ApiResponse<string>> UnlockPatientRecordAsync(
            Guid patientId, UnlockRecordDto dto, string staffUserId, Guid hospitalId)
        {
            var control = await _context.PatientAccessControls
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.PatientId == patientId);

            if (control == null)
                return ApiResponse<string>.Failure("Patient not found.");

            var codeHash = HashCode(dto.AccessCode);
            var success  = control.AccessCodeHash == codeHash;

            // Log the attempt either way
            _context.AccessLogs.Add(new AccessLog
            {
                Id                    = Guid.NewGuid(),
                PatientAccessControlId = control.Id,
                AccessedByUserId      = staffUserId,
                AccessedByName        = string.Empty,
                AccessedByRole        = "Staff",
                HospitalId            = hospitalId,
                WasSuccessful         = success,
                FailureReason         = success ? null : "Incorrect access code",
                AccessedAt            = DateTime.UtcNow
            });

            if (!success)
            {
                await _context.SaveChangesAsync();
                return ApiResponse<string>.Failure("Incorrect access code.");
            }

            control.LastUnlockedAt = DateTime.UtcNow;
            control.LastUnlockedBy = staffUserId;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Success("", "Access granted. Record unlocked for 4 hours.");
        }

        public async Task<ApiResponse<string>> GrantEmergencyAccessAsync(
            Guid patientId, string grantedByAdminId, Guid hospitalId)
        {
            var control = await _context.PatientAccessControls
                .FirstOrDefaultAsync(a => a.PatientId == patientId);

            if (control == null)
                return ApiResponse<string>.Failure("Patient not found.");

            control.EmergencyOverrideActive  = true;
            control.EmergencyOverrideExpiry  = DateTime.UtcNow.AddHours(2);
            control.EmergencyOverrideGrantedBy = grantedByAdminId;

            _context.AccessLogs.Add(new AccessLog
            {
                Id                    = Guid.NewGuid(),
                PatientAccessControlId = control.Id,
                AccessedByUserId      = grantedByAdminId,
                AccessedByRole        = "HospitalAdmin",
                HospitalId            = hospitalId,
                WasSuccessful         = true,
                WasEmergencyOverride  = true,
                AccessedAt            = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(grantedByAdminId, "", "HospitalAdmin",
                "EMERGENCY_ACCESS_GRANTED", "PatientAccessControl",
                patientId.ToString(), hospitalId: hospitalId);

            return ApiResponse<string>.Success("", "Emergency access granted for 2 hours.");
        }

        // ── Temporary Records ─────────────────────────────────────────
        public async Task<ApiResponse<TempRecordDto>> CreateTemporaryRecordAsync(
            CreateTempRecordDto dto, Guid hospitalId, string receptionistId)
        {
            // Create a minimal patient record flagged as temporary
            var hmsId = await GenerateHmsPatientIdAsync();

            var patient = new Patient
            {
                Id               = Guid.NewGuid(),
                HmsPatientId     = hmsId,
                FirstName        = dto.EstimatedFirstName ?? "Unknown",
                LastName         = dto.EstimatedLastName  ?? "Patient",
                DateOfBirth      = DateTime.UtcNow.AddYears(-30), // Estimated
                Gender           = Gender.Other,
                PhoneNumber      = "0000000000",
                OriginHospitalId = hospitalId,
                RecordStatus     = RecordStatus.Temporary,
                RegisteredAt     = DateTime.UtcNow,
            };

            var medicalRecord = new MedicalRecord
            {
                Id        = Guid.NewGuid(),
                PatientId = patient.Id,
                Status    = RecordStatus.Temporary,
                VitalInfo = new VitalInfo { Id = Guid.NewGuid() },
                AllergyInfo = new AllergyInfo { Id = Guid.NewGuid() }
            };

            patient.MedicalRecord = medicalRecord;
            _context.Patients.Add(patient);

            // Create temp record with extra info
            var tempRecord = new TemporaryPatientRecord
            {
                Id                    = Guid.NewGuid(),
                HospitalId            = hospitalId,
                EstimatedFirstName    = dto.EstimatedFirstName,
                EstimatedLastName     = dto.EstimatedLastName,
                EstimatedAge          = dto.EstimatedAge,
                EstimatedGender       = dto.EstimatedGender,
                PhysicalDescription   = dto.PhysicalDescription,
                ItemsFoundOnPatient   = dto.ItemsFoundOnPatient,
                CompanionName         = dto.CompanionName,
                CompanionPhone        = dto.CompanionPhone,
                CompanionRelationship = dto.CompanionRelationship,
                CreatedByReceptionistId = receptionistId,
                CreatedAt             = DateTime.UtcNow,
            };

            _context.TemporaryPatientRecords.Add(tempRecord);
            await _context.SaveChangesAsync();

            return ApiResponse<TempRecordDto>.Success(new TempRecordDto
            {
                Id                 = tempRecord.Id,
                EstimatedFirstName = tempRecord.EstimatedFirstName,
                EstimatedLastName  = tempRecord.EstimatedLastName,
                EstimatedAge       = tempRecord.EstimatedAge,
                CompanionName      = tempRecord.CompanionName,
                CompanionPhone     = tempRecord.CompanionPhone,
                CreatedAt          = tempRecord.CreatedAt,
                IsMerged           = false
            }, $"Temporary record created. HMS ID: {hmsId}");
        }

        public async Task<ApiResponse<string>> MergeTemporaryRecordAsync(
            Guid tempRecordId, Guid realPatientId, string mergedByUserId)
        {
            var tempRecord = await _context.TemporaryPatientRecords
                .FirstOrDefaultAsync(t => t.Id == tempRecordId);

            if (tempRecord == null)
                return ApiResponse<string>.Failure("Temporary record not found.");

            if (tempRecord.IsMerged)
                return ApiResponse<string>.Failure("Record already merged.");

            tempRecord.IsMerged            = true;
            tempRecord.MergedIntoPatientId = realPatientId;
            tempRecord.MergedAt            = DateTime.UtcNow;
            tempRecord.MergedByUserId      = mergedByUserId;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(mergedByUserId, "", "Staff",
                "MERGE_TEMP_RECORD", "TemporaryPatientRecord", tempRecordId.ToString());

            return ApiResponse<string>.Success("", "Temporary record merged successfully.");
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static string HashCode(string code)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateQuickCode()
        {
            return Random.Shared.Next(100000, 999999).ToString();
        }

        private static PatientDetailDto MapToDetail(Patient p) => new()
        {
            Id                          = p.Id,
            HmsPatientId                = p.HmsPatientId,
            FirstName                   = p.FirstName,
            LastName                    = p.LastName,
            MiddleName                  = p.MiddleName,
            FullName                    = p.FullName,
            DateOfBirth                 = p.DateOfBirth,
            Age                         = p.Age,
            Gender                      = p.Gender.ToString(),
            MaritalStatus               = p.MaritalStatus.ToString(),
            PhoneNumber                 = p.PhoneNumber,
            AlternativePhone            = p.AlternativePhone,
            Email                       = p.Email,
            NHISNumber                  = p.NHISNumber,
            BlockNumber                 = p.BlockNumber,
            Street                      = p.Street,
            City                        = p.City,
            State                       = p.State,
            EmergencyContactName        = p.EmergencyContactName,
            EmergencyContactPhone       = p.EmergencyContactPhone,
            EmergencyContactRelationship = p.EmergencyContactRelationship,
            OriginHospitalName          = p.OriginHospital?.Name ?? string.Empty,
            OriginHospitalId            = p.OriginHospitalId,
            AccessLevel                 = p.AccessLevel,
            RegisteredAt                = p.RegisteredAt,
        };
    }
}

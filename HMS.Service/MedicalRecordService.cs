using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Medical;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class MedicalRecordService : IMedicalRecordService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService        _auditService;

        public MedicalRecordService(
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _context      = context;
            _auditService = auditService;
        }

        // ── Get Medical Record ────────────────────────────────────────
        public async Task<ApiResponse<MedicalRecordDto>> GetMedicalRecordAsync(
            Guid patientId, string requestingUserId, string requestingUserRole)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.VitalInfo)
                    .ThenInclude(v => v!.VitalReadings.OrderByDescending(r => r.RecordedAt).Take(10))
                .Include(m => m.AllergyInfo)
                .Include(m => m.Prescriptions.Where(p => p.IsActive))
                .Include(m => m.DoctorNotes.OrderByDescending(n => n.CreatedAt).Take(20))
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<MedicalRecordDto>.Failure("Medical record not found.");

            var isDoctor = requestingUserRole is "Doctor" or "HospitalAdmin";

            var dto = new MedicalRecordDto
            {
                Id           = record.Id,
                PatientId    = record.PatientId,
                PatientName  = record.Patient?.FullName ?? string.Empty,
                HmsPatientId = record.Patient?.HmsPatientId ?? string.Empty,
                Status       = record.Status.ToString(),
                LastUpdated  = record.LastUpdated,

                Vitals = record.VitalInfo == null ? null : MapVitalInfo(record.VitalInfo),

                AllergyInfo = record.AllergyInfo == null ? null : new AllergyInfoDto
                {
                    HasKnownAllergies  = record.AllergyInfo.HasKnownAllergies,
                    AllergyDetails     = record.AllergyInfo.AllergyDetails,
                    ChronicConditions  = record.AllergyInfo.ChronicConditions,
                    CurrentMedications = record.AllergyInfo.CurrentMedications,
                    SurgicalHistory    = record.AllergyInfo.SurgicalHistory,
                    FamilyHistory      = record.AllergyInfo.FamilyHistory,
                    LastUpdatedAt      = record.AllergyInfo.LastUpdatedAt,
                },

                // Doctor notes — ONLY visible to doctors and admins
                DoctorNotes = isDoctor
                    ? record.DoctorNotes.Select(MapDoctorNote).ToList()
                    : null,

                Prescriptions = record.Prescriptions.Select(p => new PrescriptionDto
                {
                    Id             = p.Id,
                    MedicationName = p.MedicationName,
                    Dosage         = p.Dosage,
                    Frequency      = p.Frequency,
                    Duration       = p.Duration,
                    Instructions   = p.Instructions,
                    DoctorName     = p.DoctorName,
                    IsActive       = p.IsActive,
                    PrescribedAt   = p.PrescribedAt,
                }).ToList(),
            };

            return ApiResponse<MedicalRecordDto>.Success(dto);
        }

        // ── Update Vitals — Receptionist/Nurse ───────────────────────
        public async Task<ApiResponse<VitalInfoDto>> UpdateVitalsAsync(
            Guid patientId, Guid visitId,
            UpdateVitalsDto dto,
            string updatedByUserId, string updatedByName)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.VitalInfo)
                    .ThenInclude(v => v!.VitalReadings)
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<VitalInfoDto>.Failure("Medical record not found.");

            if (record.VitalInfo == null)
            {
                record.VitalInfo = new VitalInfo { Id = Guid.NewGuid(), MedicalRecordId = record.Id };
                _context.VitalInfos.Add(record.VitalInfo);
            }

            var vital = record.VitalInfo;

            // Update latest snapshot
            if (dto.BloodPressure       != null) vital.BloodPressure       = dto.BloodPressure;
            if (dto.TemperatureCelsius  != null) vital.TemperatureCelsius  = dto.TemperatureCelsius;
            if (dto.PulseRateBpm        != null) vital.PulseRateBpm        = dto.PulseRateBpm;
            if (dto.RespiratoryRate     != null) vital.RespiratoryRate     = dto.RespiratoryRate;
            if (dto.OxygenSaturation    != null) vital.OxygenSaturation    = dto.OxygenSaturation;
            if (dto.WeightKg            != null) vital.WeightKg            = dto.WeightKg;
            if (dto.HeightCm            != null) vital.HeightCm            = dto.HeightCm;
            if (dto.BloodGroup          != null) vital.BloodGroup          = dto.BloodGroup.Value;
            if (dto.Genotype            != null) vital.Genotype            = dto.Genotype;

            vital.LastUpdatedBy = updatedByUserId;
            vital.LastUpdatedAt = DateTime.UtcNow;

            // Also store a historical reading entry
            var reading = new VitalReading
            {
                Id                    = Guid.NewGuid(),
                VitalInfoId           = vital.Id,
                VisitId               = visitId,
                BloodPressure         = dto.BloodPressure,
                TemperatureCelsius    = dto.TemperatureCelsius,
                PulseRateBpm          = dto.PulseRateBpm,
                RespiratoryRate       = dto.RespiratoryRate,
                OxygenSaturation      = dto.OxygenSaturation,
                WeightKg              = dto.WeightKg,
                RecordedByUserId      = updatedByUserId,
                RecordedByName        = updatedByName,
                RecordedAtHospitalId  = Guid.Empty, // filled by controller from claim
                RecordedAt            = DateTime.UtcNow
            };

            vital.VitalReadings.Add(reading);

            record.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                updatedByUserId, updatedByName, "Receptionist",
                "UPDATE_VITALS", "VitalInfo", vital.Id.ToString(),
                hospitalId: null);

            return ApiResponse<VitalInfoDto>.Success(
                MapVitalInfo(vital), "Vitals updated successfully.");
        }

        // ── Add Doctor Note — Doctor only ────────────────────────────
        public async Task<ApiResponse<DoctorNoteDto>> AddDoctorNoteAsync(
            Guid patientId, Guid visitId,
            AddDoctorNoteDto dto,
            string doctorUserId, string doctorName,
            Guid hospitalId, string hospitalName)
        {
            var record = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<DoctorNoteDto>.Failure("Medical record not found.");

            // Check this doctor is assigned to the visit
            var visit = await _context.HospitalVisits
                .FirstOrDefaultAsync(v => v.Id == visitId &&
                                          v.AssignedDoctorId == doctorUserId);
            if (visit == null)
                return ApiResponse<DoctorNoteDto>.Failure(
                    "You are not the assigned doctor for this visit.");

            var note = new DoctorNote
            {
                Id                  = Guid.NewGuid(),
                MedicalRecordId     = record.Id,
                VisitId             = visitId,
                DoctorUserId        = doctorUserId,
                DoctorName          = doctorName,
                HospitalId          = hospitalId,
                HospitalName        = hospitalName,
                ChiefComplaint      = dto.ChiefComplaint,
                History             = dto.History,
                Examination         = dto.Examination,
                Diagnosis           = dto.Diagnosis,
                TreatmentPlan       = dto.TreatmentPlan,
                FollowUpInstructions = dto.FollowUpInstructions,
                Referral            = dto.Referral,
                IsConfidential      = true,
                CreatedAt           = DateTime.UtcNow,
            };

            _context.DoctorNotes.Add(note);
            record.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                doctorUserId, doctorName, "Doctor",
                "ADD_DOCTOR_NOTE", "DoctorNote", note.Id.ToString(),
                hospitalId: hospitalId);

            return ApiResponse<DoctorNoteDto>.Success(
                MapDoctorNote(note), "Note added successfully.");
        }

        // ── Update Doctor Note — only within 24 hours ─────────────────
        public async Task<ApiResponse<DoctorNoteDto>> UpdateDoctorNoteAsync(
            Guid noteId, UpdateDoctorNoteDto dto, string doctorUserId)
        {
            var note = await _context.DoctorNotes
                .FirstOrDefaultAsync(n => n.Id == noteId &&
                                          n.DoctorUserId == doctorUserId);

            if (note == null)
                return ApiResponse<DoctorNoteDto>.Failure("Note not found.");

            if (!note.IsEditable)
                return ApiResponse<DoctorNoteDto>.Failure(
                    "Notes can only be edited within 24 hours of creation.");

            if (dto.ChiefComplaint      != null) note.ChiefComplaint      = dto.ChiefComplaint;
            if (dto.History             != null) note.History             = dto.History;
            if (dto.Examination         != null) note.Examination         = dto.Examination;
            if (dto.Diagnosis           != null) note.Diagnosis           = dto.Diagnosis;
            if (dto.TreatmentPlan       != null) note.TreatmentPlan       = dto.TreatmentPlan;
            if (dto.FollowUpInstructions != null) note.FollowUpInstructions = dto.FollowUpInstructions;
            if (dto.Referral            != null) note.Referral            = dto.Referral;

            note.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<DoctorNoteDto>.Success(
                MapDoctorNote(note), "Note updated.");
        }

        // ── Update Allergy Info ───────────────────────────────────────
        public async Task<ApiResponse<AllergyInfoDto>> UpdateAllergyInfoAsync(
            Guid patientId, UpdateAllergyInfoDto dto, string updatedByUserId)
        {
            var record = await _context.MedicalRecords
                .Include(m => m.AllergyInfo)
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<AllergyInfoDto>.Failure("Medical record not found.");

            if (record.AllergyInfo == null)
            {
                record.AllergyInfo = new AllergyInfo
                {
                    Id              = Guid.NewGuid(),
                    MedicalRecordId = record.Id
                };
                _context.AllergyInfos.Add(record.AllergyInfo);
            }

            var allergy              = record.AllergyInfo;
            allergy.HasKnownAllergies  = dto.HasKnownAllergies;
            allergy.AllergyDetails     = dto.AllergyDetails;
            allergy.ChronicConditions  = dto.ChronicConditions;
            allergy.CurrentMedications = dto.CurrentMedications;
            allergy.SurgicalHistory    = dto.SurgicalHistory;
            allergy.FamilyHistory      = dto.FamilyHistory;
            allergy.LastUpdatedBy      = updatedByUserId;
            allergy.LastUpdatedAt      = DateTime.UtcNow;

            record.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<AllergyInfoDto>.Success(new AllergyInfoDto
            {
                HasKnownAllergies  = allergy.HasKnownAllergies,
                AllergyDetails     = allergy.AllergyDetails,
                ChronicConditions  = allergy.ChronicConditions,
                CurrentMedications = allergy.CurrentMedications,
                SurgicalHistory    = allergy.SurgicalHistory,
                FamilyHistory      = allergy.FamilyHistory,
                LastUpdatedAt      = allergy.LastUpdatedAt,
            }, "Allergy info updated.");
        }

        // ── Add Prescription — Doctor only ────────────────────────────
        public async Task<ApiResponse<PrescriptionDto>> AddPrescriptionAsync(
            Guid patientId, Guid visitId,
            AddPrescriptionDto dto,
            string doctorUserId, string doctorName, Guid hospitalId)
        {
            var record = await _context.MedicalRecords
                .FirstOrDefaultAsync(m => m.PatientId == patientId);

            if (record == null)
                return ApiResponse<PrescriptionDto>.Failure("Medical record not found.");

            var visit = await _context.HospitalVisits
                .FirstOrDefaultAsync(v => v.Id == visitId &&
                                          v.AssignedDoctorId == doctorUserId);
            if (visit == null)
                return ApiResponse<PrescriptionDto>.Failure(
                    "You are not the assigned doctor for this visit.");

            var prescription = new Prescription
            {
                Id             = Guid.NewGuid(),
                MedicalRecordId = record.Id,
                VisitId        = visitId,
                DoctorUserId   = doctorUserId,
                DoctorName     = doctorName,
                HospitalId     = hospitalId,
                MedicationName = dto.MedicationName,
                Dosage         = dto.Dosage,
                Frequency      = dto.Frequency,
                Duration       = dto.Duration,
                Instructions   = dto.Instructions,
                IsActive       = true,
                PrescribedAt   = DateTime.UtcNow,
            };

            _context.Prescriptions.Add(prescription);
            record.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<PrescriptionDto>.Success(new PrescriptionDto
            {
                Id             = prescription.Id,
                MedicationName = prescription.MedicationName,
                Dosage         = prescription.Dosage,
                Frequency      = prescription.Frequency,
                Duration       = prescription.Duration,
                Instructions   = prescription.Instructions,
                DoctorName     = prescription.DoctorName,
                IsActive       = prescription.IsActive,
                PrescribedAt   = prescription.PrescribedAt,
            }, "Prescription added.");
        }

        // ── Helpers ───────────────────────────────────────────────────
        private static VitalInfoDto MapVitalInfo(VitalInfo v) => new()
        {
            BloodGroup         = v.BloodGroup.ToString(),
            Genotype           = v.Genotype,
            HeightCm           = v.HeightCm,
            WeightKg           = v.WeightKg,
            BMI                = v.BMI,
            BloodPressure      = v.BloodPressure,
            TemperatureCelsius = v.TemperatureCelsius,
            PulseRateBpm       = v.PulseRateBpm,
            RespiratoryRate    = v.RespiratoryRate,
            OxygenSaturation   = v.OxygenSaturation,
            LastUpdatedAt      = v.LastUpdatedAt,
            LastUpdatedBy      = v.LastUpdatedBy,
            ReadingHistory     = v.VitalReadings.Select(r => new VitalReadingDto
            {
                BloodPressure      = r.BloodPressure,
                TemperatureCelsius = r.TemperatureCelsius,
                PulseRateBpm       = r.PulseRateBpm,
                WeightKg           = r.WeightKg,
                RecordedBy         = r.RecordedByName,
                RecordedAt         = r.RecordedAt,
                HospitalName       = string.Empty,
            }).ToList()
        };

        private static DoctorNoteDto MapDoctorNote(DoctorNote n) => new()
        {
            Id                   = n.Id,
            DoctorName           = n.DoctorName,
            HospitalName         = n.HospitalName,
            ChiefComplaint       = n.ChiefComplaint,
            History              = n.History,
            Examination          = n.Examination,
            Diagnosis            = n.Diagnosis,
            TreatmentPlan        = n.TreatmentPlan,
            FollowUpInstructions = n.FollowUpInstructions,
            Referral             = n.Referral,
            IsEditable           = n.IsEditable,
            CreatedAt            = n.CreatedAt,
            LastUpdated          = n.LastUpdated,
        };
    }
}

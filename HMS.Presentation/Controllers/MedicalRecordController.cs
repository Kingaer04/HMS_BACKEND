using HMS.Service.Contracts;
using HMS.Shared.DTOs.Medical;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Patient medical record — vitals, allergy info, doctor notes, prescriptions.
    /// Access rules enforced per section: doctors see everything, others see only their section.
    /// </summary>
    [ApiController]
    [Route("api/medical-records")]
    [Authorize]
    [Produces("application/json")]
    public class MedicalRecordController : ControllerBase
    {
        private readonly IMedicalRecordService _medicalRecordService;

        public MedicalRecordController(IMedicalRecordService medicalRecordService)
        {
            _medicalRecordService = medicalRecordService;
        }

        private string UserId   => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
        private string UserName => $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
        private Guid HospitalId => Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;

        // ── Get Full Record ───────────────────────────────────────────
        /// <summary>
        /// Get a patient's full medical record.
        /// Doctor notes section is ONLY returned when the requester is a Doctor or HospitalAdmin.
        /// Receptionist, LabTechnician and Patient see vitals, allergy info and prescriptions only.
        /// </summary>
        [HttpGet("patient/{patientId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalRecordDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetRecord(Guid patientId)
        {
            var result = await _medicalRecordService
                .GetMedicalRecordAsync(patientId, UserId, UserRole);

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Update Vitals ─────────────────────────────────────────────
        /// <summary>
        /// Update a patient's vital signs for a visit.
        /// Only Receptionist (nurse) and HospitalAdmin can update vitals.
        /// Every update creates a historical reading entry — doctors can see the trend.
        /// Fields not provided are left unchanged.
        /// </summary>
        [HttpPut("patient/{patientId:guid}/vitals/{visitId:guid}")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<VitalInfoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> UpdateVitals(
            Guid patientId, Guid visitId, [FromBody] UpdateVitalsDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _medicalRecordService
                .UpdateVitalsAsync(patientId, visitId, dto, UserId, UserName);

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Add Doctor Note ───────────────────────────────────────────
        /// <summary>
        /// Add a clinical note to the patient's medical record.
        /// Only the ASSIGNED doctor for the current visit can add notes.
        /// Notes are confidential — not visible to receptionist, lab, or patient.
        /// </summary>
        [HttpPost("patient/{patientId:guid}/notes/{visitId:guid}")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<DoctorNoteDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> AddDoctorNote(
            Guid patientId, Guid visitId, [FromBody] AddDoctorNoteDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var hospitalName = User.FindFirst("HospitalName")?.Value ?? string.Empty;

            var result = await _medicalRecordService.AddDoctorNoteAsync(
                patientId, visitId, dto,
                UserId, UserName, HospitalId, hospitalName);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        // ── Update Doctor Note ────────────────────────────────────────
        /// <summary>
        /// Update an existing doctor note.
        /// Only the original author can edit, and only within 24 hours of creation.
        /// </summary>
        [HttpPut("notes/{noteId:guid}")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<DoctorNoteDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> UpdateDoctorNote(
            Guid noteId, [FromBody] UpdateDoctorNoteDto dto)
        {
            var result = await _medicalRecordService
                .UpdateDoctorNoteAsync(noteId, dto, UserId);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Update Allergy Info ───────────────────────────────────────
        /// <summary>
        /// Update a patient's allergy and medical history information.
        /// Receptionist, Doctor, and HospitalAdmin can update this section.
        /// </summary>
        [HttpPut("patient/{patientId:guid}/allergy-info")]
        [Authorize(Roles = "Receptionist,Doctor,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<AllergyInfoDto>), 200)]
        public async Task<IActionResult> UpdateAllergyInfo(
            Guid patientId, [FromBody] UpdateAllergyInfoDto dto)
        {
            var result = await _medicalRecordService
                .UpdateAllergyInfoAsync(patientId, dto, UserId);

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Add Prescription ──────────────────────────────────────────
        /// <summary>
        /// Add a prescription to a patient's record during a visit.
        /// Only the assigned doctor for the visit can prescribe medication.
        /// </summary>
        [HttpPost("patient/{patientId:guid}/prescriptions/{visitId:guid}")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<PrescriptionDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> AddPrescription(
            Guid patientId, Guid visitId, [FromBody] AddPrescriptionDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _medicalRecordService.AddPrescriptionAsync(
                patientId, visitId, dto, UserId, UserName, HospitalId);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }
    }
}

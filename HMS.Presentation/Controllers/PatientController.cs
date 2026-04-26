using HMS.Service.Contracts;
using HMS.Shared.DTOs.Patient;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Patient management — registration, search, access control, temporary records.
    /// </summary>
    [ApiController]
    [Route("api/patients")]
    [Authorize]
    [Produces("application/json")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientService _patientService;

        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
        }

        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private Guid HospitalId => Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;
        private string UserName => $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();

        // ── Search ────────────────────────────────────────────────────
        /// <summary>
        /// Search patients by HMS Patient ID, name, or phone number.
        /// Returns patients from all hospitals (centralized database).
        /// </summary>
        /// <param name="query">Search term — HMS ID, name, or phone</param>
        [HttpGet("search")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientSummaryDto>>), 200)]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest(ApiResponse<string>.Failure("Search query must be at least 2 characters."));

            var result = await _patientService.SearchPatientsAsync(query, HospitalId);
            return Ok(result);
        }

        // ── Get by ID ─────────────────────────────────────────────────
        /// <summary>
        /// Get full patient profile. Locked records require prior unlock.
        /// </summary>
        [HttpGet("{patientId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 403)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetById(Guid patientId)
        {
            var result = await _patientService.GetPatientByIdAsync(patientId, UserId);
            if (!result.IsSuccess) return result.Message!.Contains("locked")
                ? StatusCode(403, result) : NotFound(result);
            return Ok(result);
        }

        // ── Register ──────────────────────────────────────────────────
        /// <summary>
        /// Register a new patient at this hospital.
        /// The system generates a unique HMS Patient ID (HMS-YYYY-NNNNNN).
        /// Only Receptionist or HospitalAdmin can register patients.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Register([FromBody] RegisterPatientDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _patientService.RegisterPatientAsync(dto, HospitalId, UserId);
            if (!result.IsSuccess) return BadRequest(result);

            return StatusCode(201, result);
        }

        // ── Update ────────────────────────────────────────────────────
        /// <summary>Update a patient's personal information.</summary>
        [HttpPut("{patientId:guid}")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> Update(Guid patientId, [FromBody] UpdatePatientDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var result = await _patientService.UpdatePatientAsync(patientId, dto, UserId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Lock Record ───────────────────────────────────────────────
        /// <summary>
        /// Lock a patient's medical record with an access code.
        /// Patient controls this from the patient app or at reception.
        /// Returns a 6-digit quick reference code.
        /// </summary>
        [HttpPost("{patientId:guid}/lock")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> LockRecord(Guid patientId, [FromBody] LockRecordDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var result = await _patientService.LockPatientRecordAsync(patientId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Unlock Record ─────────────────────────────────────────────
        /// <summary>
        /// Unlock a patient record using the patient's access code.
        /// All unlock attempts (success or failure) are logged.
        /// Access is valid for 4 hours after unlock.
        /// </summary>
        [HttpPost("{patientId:guid}/unlock")]
        [Authorize(Roles = "Receptionist,HospitalAdmin,Doctor")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> UnlockRecord(Guid patientId, [FromBody] UnlockRecordDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var result = await _patientService.UnlockPatientRecordAsync(patientId, dto, UserId, HospitalId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Emergency Access ──────────────────────────────────────────
        /// <summary>
        /// Grant emergency override access to a locked record.
        /// Only HospitalAdmin can do this. Fully audited.
        /// Override expires in 2 hours.
        /// </summary>
        [HttpPost("{patientId:guid}/emergency-access")]
        [Authorize(Roles = "HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> GrantEmergencyAccess(Guid patientId)
        {
            var result = await _patientService.GrantEmergencyAccessAsync(patientId, UserId, HospitalId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Temporary / Emergency Patient ─────────────────────────────
        /// <summary>
        /// Create a temporary patient record for an unidentified emergency patient.
        /// A temporary HMS ID is generated. Record can be merged with real record later.
        /// </summary>
        [HttpPost("temporary")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<TempRecordDto>), 201)]
        public async Task<IActionResult> CreateTemporary([FromBody] CreateTempRecordDto dto)
        {
            var result = await _patientService.CreateTemporaryRecordAsync(dto, HospitalId, UserId);
            return StatusCode(201, result);
        }

        // ── Merge Temporary Record ────────────────────────────────────
        /// <summary>
        /// Merge a temporary emergency record into an identified patient's real record.
        /// </summary>
        [HttpPost("temporary/{tempRecordId:guid}/merge/{realPatientId:guid}")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> MergeTemporary(Guid tempRecordId, Guid realPatientId)
        {
            var result = await _patientService.MergeTemporaryRecordAsync(tempRecordId, realPatientId, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}

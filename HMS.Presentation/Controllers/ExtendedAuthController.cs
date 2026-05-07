using HMS.Service.Contracts;
using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Extended authentication — Lab Technician and Patient app registration/login.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class ExtendedAuthController : ControllerBase
    {
        private readonly IExtendedAuthService _extendedAuthService;

        public ExtendedAuthController(IExtendedAuthService extendedAuthService)
        {
            _extendedAuthService = extendedAuthService;
        }

        private Guid HospitalId => Guid.TryParse(
            User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;

        // ── Lab Technician ────────────────────────────────────────────
        /// <summary>
        /// Register a lab technician. Only HospitalAdmin can call this.
        /// HospitalId is taken from the admin JWT — not from the request body.
        /// </summary>
        [HttpPost("register/lab-technician")]
        [Authorize(Roles = "HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> RegisterLabTechnician(
            [FromBody] RegisterLabTechnicianDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            dto.HospitalId = HospitalId;

            var result = await _extendedAuthService.RegisterLabTechnicianAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Registration failed.", result.Errors));

            return StatusCode(201, ApiResponse<AuthResponseDto>.Success(result));
        }

        /// <summary>
        /// Lab technician login using Email + Password.
        /// </summary>
        [HttpPost("login/lab-technician")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 401)]
        public async Task<IActionResult> LoginLabTechnician(
            [FromBody] LoginLabTechnicianDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _extendedAuthService.LoginLabTechnicianAsync(dto);
            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Login failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }

        // ── Patient App ───────────────────────────────────────────────
        /// <summary>
        /// Register a patient account on the patient app.
        ///
        /// Prerequisites:
        /// - Patient must already exist in the system (registered by a receptionist)
        /// - Patient needs their HMS Patient ID (e.g. HMS-2025-000001)
        /// - Date of birth must match the hospital record (verification)
        ///
        /// After registration the patient can:
        /// - Book appointments at any hospital
        /// - View their own medical records
        /// - Lock/unlock their record with an access code
        /// </summary>
        [HttpPost("register/patient")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> RegisterPatientApp(
            [FromBody] RegisterPatientAppDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _extendedAuthService.RegisterPatientAppAsync(dto);
            if (!result.IsSuccess)
                return BadRequest(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Registration failed.", result.Errors));

            return StatusCode(201, ApiResponse<AuthResponseDto>.Success(result));
        }

        /// <summary>
        /// Patient app login using Email + Password.
        /// </summary>
        [HttpPost("login/patient")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 401)]
        public async Task<IActionResult> LoginPatient([FromBody] LoginPatientDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _extendedAuthService.LoginPatientAsync(dto);
            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Login failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }
    }
}

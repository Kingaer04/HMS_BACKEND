using HMS.Service.Contracts;
using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.Presentation.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ── NHIS UID Verification ────────────────────────────────────────
        /// <summary>
        /// Step 1 of hospital registration.
        /// Verify the NHIS UID before proceeding to full registration.
        /// </summary>
        [HttpPost("verify-uid")]
        public async Task<IActionResult> VerifyHospitalUID([FromBody] VerifyHospitalUIDDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var result = await _authService.VerifyHospitalUIDAsync(dto.HospitalUID);

            if (!result.IsValid)
                return BadRequest(ApiResponse<NhisVerificationResponseDto>
                    .Failure(result.Message));

            return Ok(ApiResponse<NhisVerificationResponseDto>
                .Success(result, result.Message));
        }

        // ── Hospital Registration ────────────────────────────────────────
        /// <summary>
        /// Step 2 of hospital registration.
        /// NHIS UID is re-verified server-side before the account is created.
        /// </summary>
        [HttpPost("register/hospital")]
        public async Task<IActionResult> RegisterHospital([FromBody] RegisterHospitalDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var result = await _authService.RegisterHospitalAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Registration failed.", result.Errors));

            return StatusCode(201, ApiResponse<AuthResponseDto>
                .Success(result, "Hospital registered successfully."));
        }

        // ── Hospital Login (UID + Password) ──────────────────────────────
        [HttpPost("login/hospital")]
        public async Task<IActionResult> LoginHospital([FromBody] LoginHospitalDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var result = await _authService.LoginHospitalAsync(dto);

            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Login failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }

        // ── Doctor Registration ──────────────────────────────────────────
        /// <summary>
        /// Register a doctor. Only the HospitalAdmin can call this.
        /// HospitalId is extracted from the admin JWT — not from the request body.
        /// </summary>
        [HttpPost("register/doctor")]
        [Authorize(Roles = "HospitalAdmin")]
        public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            // Pull HospitalId from the logged-in admin's token
            var hospitalIdClaim = User.FindFirst("HospitalId")?.Value;
            if (string.IsNullOrEmpty(hospitalIdClaim) ||
                !Guid.TryParse(hospitalIdClaim, out var hospitalId))
                return Unauthorized("Could not determine hospital from token.");

            dto.HospitalId = hospitalId;

            var result = await _authService.RegisterDoctorAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Registration failed.", result.Errors));

            return StatusCode(201, ApiResponse<AuthResponseDto>
                .Success(result, "Doctor registered successfully."));
        }

        // ── Doctor Login (Email + Password) ──────────────────────────────
        [HttpPost("login/doctor")]
        public async Task<IActionResult> LoginDoctor([FromBody] LoginDoctorDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var result = await _authService.LoginDoctorAsync(dto);

            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Login failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }

        // ── Receptionist Registration ─────────────────────────────────────
        /// <summary>
        /// Register a receptionist. Only the HospitalAdmin can call this.
        /// HospitalId is extracted from the admin JWT — not from the request body.
        /// </summary>
        [HttpPost("register/receptionist")]
        [Authorize(Roles = "HospitalAdmin")]
        public async Task<IActionResult> RegisterReceptionist([FromBody] RegisterReceptionistDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var hospitalIdClaim = User.FindFirst("HospitalId")?.Value;
            if (string.IsNullOrEmpty(hospitalIdClaim) ||
                !Guid.TryParse(hospitalIdClaim, out var hospitalId))
                return Unauthorized("Could not determine hospital from token.");

            dto.HospitalId = hospitalId;

            var result = await _authService.RegisterReceptionistAsync(dto);

            if (!result.IsSuccess)
                return BadRequest(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Registration failed.", result.Errors));

            return StatusCode(201, ApiResponse<AuthResponseDto>
                .Success(result, "Receptionist registered successfully."));
        }

        // ── Receptionist Login (Email + Password) ─────────────────────────
        [HttpPost("login/receptionist")]
        public async Task<IActionResult> LoginReceptionist([FromBody] LoginReceptionistDto dto)
        {
            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var result = await _authService.LoginReceptionistAsync(dto);

            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Login failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }

        // ── Refresh Token ─────────────────────────────────────────────────
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);

            if (!result.IsSuccess)
                return Unauthorized(ApiResponse<AuthResponseDto>
                    .Failure(result.Message ?? "Token refresh failed."));

            return Ok(ApiResponse<AuthResponseDto>.Success(result));
        }

        // ── Logout ────────────────────────────────────────────────────────
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId is not null)
                await _authService.RevokeTokenAsync(userId);

            return Ok(new { message = "Logged out successfully." });
        }
    }
}

using HMS.Service.Contracts;
using HMS.Shared.DTOs.Admin;
using HMS.Shared.DTOs.Profile;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Profile management — view and update own profile, change password.
    /// </summary>
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    [Produces("application/json")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        private string UserId   => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string UserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        /// <summary>
        /// Get the profile of the currently logged-in user.
        /// Returns DoctorProfileDto for doctors, StaffProfileDto for others.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<DoctorProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<StaffProfileDto>), 200)]
        public async Task<IActionResult> GetMyProfile()
        {
            if (UserRole == "Doctor")
            {
                var result = await _profileService.GetDoctorProfileAsync(UserId);
                if (!result.IsSuccess) return NotFound(result);
                return Ok(result);
            }
            else
            {
                var result = await _profileService.GetStaffProfileAsync(UserId);
                if (!result.IsSuccess) return NotFound(result);
                return Ok(result);
            }
        }

        /// <summary>
        /// Get a specific doctor's profile by userId.
        /// Used by receptionist when viewing doctor info before assigning.
        /// </summary>
        [HttpGet("doctor/{userId}")]
        [ProducesResponseType(typeof(ApiResponse<DoctorProfileDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetDoctorProfile(string userId)
        {
            var result = await _profileService.GetDoctorProfileAsync(userId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Update doctor profile — specialization, availability, schedule, qualifications.
        /// Only the logged-in doctor can update their own profile.
        /// </summary>
        [HttpPut("doctor")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<DoctorProfileDto>), 200)]
        public async Task<IActionResult> UpdateDoctorProfile(
            [FromBody] UpdateDoctorProfileDto dto)
        {
            var result = await _profileService.UpdateDoctorProfileAsync(UserId, dto);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Toggle doctor availability for today.
        /// When marked unavailable the doctor won't appear in the assignment list.
        /// </summary>
        [HttpPatch("doctor/availability")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        public async Task<IActionResult> ToggleAvailability(
            [FromQuery] bool isAvailable)
        {
            var result = await _profileService
                .ToggleDoctorAvailabilityAsync(UserId, isAvailable);
            return Ok(result);
        }

        /// <summary>
        /// Update receptionist or lab technician profile.
        /// Only the logged-in staff member can update their own profile.
        /// </summary>
        [HttpPut("staff")]
        [Authorize(Roles = "Receptionist,LabTechnician")]
        [ProducesResponseType(typeof(ApiResponse<StaffProfileDto>), 200)]
        public async Task<IActionResult> UpdateStaffProfile(
            [FromBody] UpdateStaffProfileDto dto)
        {
            var result = await _profileService.UpdateStaffProfileAsync(UserId, dto);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Change the logged-in user's password.
        /// On success all existing sessions are invalidated — user must log in again.
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _profileService.ChangePasswordAsync(UserId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }

    /// <summary>
    /// Hospital admin dashboard — stats, staff management, deactivation.
    /// All endpoints require HospitalAdmin role.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "HospitalAdmin")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private string UserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private Guid HospitalId =>
            Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id)
                ? id : Guid.Empty;

        /// <summary>
        /// Get hospital dashboard statistics.
        /// Includes today's visits, active visits, pending labs,
        /// pending appointments, staff counts and revenue.
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<HospitalStatsDto>), 200)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _adminService.GetHospitalStatsAsync(HospitalId);
            return Ok(result);
        }

        /// <summary>
        /// Get all staff at this hospital.
        /// Optional role filter: Doctor, Receptionist, LabTechnician.
        /// </summary>
        [HttpGet("staff")]
        [ProducesResponseType(typeof(ApiResponse<List<StaffListItemDto>>), 200)]
        public async Task<IActionResult> GetStaff([FromQuery] string? role)
        {
            var result = await _adminService.GetStaffListAsync(HospitalId, role);
            return Ok(result);
        }

        /// <summary>
        /// Deactivate a staff member — they can no longer log in.
        /// Active sessions are immediately revoked.
        /// Cannot deactivate another HospitalAdmin.
        /// Fully audited.
        /// </summary>
        [HttpPost("staff/{staffUserId}/deactivate")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> DeactivateStaff(
            string staffUserId, [FromBody] DeactivateStaffDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _adminService
                .DeactivateStaffAsync(staffUserId, UserId, dto.Reason);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Reactivate a previously deactivated staff member.
        /// They can log in immediately after reactivation.
        /// </summary>
        [HttpPost("staff/{staffUserId}/reactivate")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> ReactivateStaff(string staffUserId)
        {
            var result = await _adminService
                .ReactivateStaffAsync(staffUserId, UserId);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}

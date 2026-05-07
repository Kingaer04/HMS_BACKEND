using HMS.Shared.DTOs.Auth;
using HMS.Shared.DTOs.Profile;
using HMS.Shared.DTOs.Admin;
using HMS.Shared.Responses;

namespace HMS.Service.Contracts
{
    public interface IExtendedAuthService
    {
        Task<AuthResponseDto> RegisterLabTechnicianAsync(RegisterLabTechnicianDto dto);
        Task<AuthResponseDto> LoginLabTechnicianAsync(LoginLabTechnicianDto dto);
        Task<AuthResponseDto> RegisterPatientAppAsync(RegisterPatientAppDto dto);
        Task<AuthResponseDto> LoginPatientAsync(LoginPatientDto dto);
    }

    public interface IProfileService
    {
        Task<ApiResponse<DoctorProfileDto>> GetDoctorProfileAsync(string userId);
        Task<ApiResponse<DoctorProfileDto>> UpdateDoctorProfileAsync(string userId, UpdateDoctorProfileDto dto);
        Task<ApiResponse<string>>           ToggleDoctorAvailabilityAsync(string userId, bool isAvailable);
        Task<ApiResponse<StaffProfileDto>>  GetStaffProfileAsync(string userId);
        Task<ApiResponse<StaffProfileDto>>  UpdateStaffProfileAsync(string userId, UpdateStaffProfileDto dto);
        Task<ApiResponse<string>>           ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }

    public interface IAdminService
    {
        Task<ApiResponse<HospitalStatsDto>>       GetHospitalStatsAsync(Guid hospitalId);
        Task<ApiResponse<List<StaffListItemDto>>> GetStaffListAsync(Guid hospitalId, string? role);
        Task<ApiResponse<string>>                 DeactivateStaffAsync(string staffUserId, string adminUserId, string reason);
        Task<ApiResponse<string>>                 ReactivateStaffAsync(string staffUserId, string adminUserId);
    }
}

using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;

namespace HMS.Service.Contracts
{
    public interface IAuthService
    {
        // Hospital
        Task<NhisVerificationResponseDto> VerifyHospitalUIDAsync(string hospitalUID);
        Task<AuthResponseDto> RegisterHospitalAsync(RegisterHospitalDto dto);
        Task<AuthResponseDto> LoginHospitalAsync(LoginHospitalDto dto);

        // Doctor
        Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto);
        Task<AuthResponseDto> LoginDoctorAsync(LoginDoctorDto dto);

        // Receptionist
        Task<AuthResponseDto> RegisterReceptionistAsync(RegisterReceptionistDto dto);
        Task<AuthResponseDto> LoginReceptionistAsync(LoginReceptionistDto dto);

        // Token
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string userId);
    }
}

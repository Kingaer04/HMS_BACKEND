using HMS.Entities.Models;
using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;

namespace HMS.Service.Contracts
{
    public interface IAuthService
    {
        Task<NhisVerificationResponseDto> VerifyHospitalUIDAsync(string hospitalUID);
        Task<AuthResponseDto> RegisterHospitalAsync(RegisterHospitalDto dto);
        Task<AuthResponseDto> LoginHospitalAsync(LoginHospitalDto dto);
        Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto);
        Task<AuthResponseDto> LoginDoctorAsync(LoginDoctorDto dto);
        Task<AuthResponseDto> RegisterReceptionistAsync(RegisterReceptionistDto dto);
        Task<AuthResponseDto> LoginReceptionistAsync(LoginReceptionistDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string userId);
    }

    public interface INhisVerificationService
    {
        /// <summary>
        /// Verifies the hospital UID. Currently mock — swap for real NHIS API later.
        /// </summary>
        Task<NhisVerificationResult> VerifyAsync(string hospitalUID);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(HMS.Entities.Models.ApplicationUser user, IList<string> roles);
        string GenerateRefreshToken();
        System.Security.Claims.ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }

    public interface ILoggerService
    {
        void LogInfo(string message);
        void LogWarn(string message);
        void LogError(string message);
        void LogDebug(string message);
    }
}

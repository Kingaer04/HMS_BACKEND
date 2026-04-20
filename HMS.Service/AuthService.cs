using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Identity;

namespace HMS.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser>  _userManager;
        private readonly INhisVerificationService      _nhisService;
        private readonly ITokenService                 _tokenService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            INhisVerificationService     nhisService,
            ITokenService                tokenService)
        {
            _userManager  = userManager;
            _nhisService  = nhisService;
            _tokenService = tokenService;
        }

        public async Task<NhisVerificationResponseDto> VerifyHospitalUIDAsync(string hospitalUID)
        {
            var result = await _nhisService.VerifyAsync(hospitalUID);
            return new NhisVerificationResponseDto
            {
                IsValid      = result.IsValid,
                HospitalName = result.HospitalName,
                Message      = result.Message ?? string.Empty
            };
        }

        public async Task<AuthResponseDto> RegisterHospitalAsync(RegisterHospitalDto dto)
        {
            // Re-verify UID server-side
            var nhis = await _nhisService.VerifyAsync(dto.HospitalUID);
            if (!nhis.IsValid)
                return Fail(nhis.Message ?? "Invalid Hospital UID.");

            // Check UID not already registered
            var uidTaken = _userManager.Users.Any(u =>
                u.Hospital != null &&
                u.Hospital.HospitalUID == dto.HospitalUID.Trim().ToUpperInvariant());
            if (uidTaken)
                return Fail("A hospital with this UID is already registered.");

            // Check admin email free
            if (await _userManager.FindByEmailAsync(dto.AdminEmail) != null)
                return Fail("An account with this admin email already exists.");

            var hospital = new Hospital
            {
                Id                     = Guid.NewGuid(),
                Name                   = dto.HospitalName,
                HospitalUID            = dto.HospitalUID.Trim().ToUpperInvariant(),
                BlockNumber            = dto.BlockNumber,
                Street                 = dto.Street,
                State                  = dto.State,
                Country                = "Nigeria",
                PhoneNumber            = dto.PhoneNumber,
                AlternativePhoneNumber = dto.AlternativePhoneNumber,
                Email                  = dto.HospitalEmail,
            };

            var admin = new ApplicationUser
            {
                FirstName   = dto.AdminFirstName,
                LastName    = dto.AdminLastName,
                Email       = dto.AdminEmail,
                UserName    = dto.AdminEmail,
                PhoneNumber = dto.AdminPhoneNumber,
                Role        = UserRole.HospitalAdmin,
                HospitalId  = hospital.Id,
                Hospital    = hospital,
            };

            var result = await _userManager.CreateAsync(admin, dto.Password);
            if (!result.Succeeded)
                return Fail("Registration failed.", result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(admin, "HospitalAdmin");
            return await BuildAuthResponseAsync(admin, "Hospital registered successfully.");
        }

        public async Task<AuthResponseDto> LoginHospitalAsync(LoginHospitalDto dto)
        {
            var admin = _userManager.Users.FirstOrDefault(u =>
                u.Hospital != null &&
                u.Hospital.HospitalUID == dto.HospitalUID.Trim().ToUpperInvariant() &&
                u.Role == UserRole.HospitalAdmin);

            if (admin == null || !await _userManager.CheckPasswordAsync(admin, dto.Password))
                return Fail("Invalid Hospital UID or password.");

            return await BuildAuthResponseAsync(admin, "Login successful.");
        }

        public async Task<AuthResponseDto> RegisterDoctorAsync(RegisterDoctorDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Fail("Email already in use.");

            var doctor = new ApplicationUser
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role = UserRole.Doctor,
                HospitalId = dto.HospitalId,
                DoctorProfile = new DoctorProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = string.Empty, // EF fills this after user is created
                    HospitalId = dto.HospitalId,
                    MedicalLicenseNumber = dto.MedicalLicenseNumber,
                    Specialization = string.Empty,
                }
            };

            var result = await _userManager.CreateAsync(doctor, dto.Password);
            if (!result.Succeeded)
                return Fail("Registration failed.", result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(doctor, "Doctor");
            return await BuildAuthResponseAsync(doctor, "Doctor registered successfully.");
        }

        public async Task<AuthResponseDto> LoginDoctorAsync(LoginDoctorDto dto)
        {
            var doctor = await _userManager.FindByEmailAsync(dto.Email);
            if (doctor == null || doctor.Role != UserRole.Doctor ||
                !await _userManager.CheckPasswordAsync(doctor, dto.Password))
                return Fail("Invalid email or password.");

            return await BuildAuthResponseAsync(doctor, "Login successful.");
        }

        public async Task<AuthResponseDto> RegisterReceptionistAsync(RegisterReceptionistDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Fail("Email already in use.");

            var receptionist = new ApplicationUser
            {
                FirstName   = dto.FirstName,
                LastName    = dto.LastName,
                Email       = dto.Email,
                UserName    = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Role        = UserRole.Receptionist,
                HospitalId  = dto.HospitalId,
            };

            var result = await _userManager.CreateAsync(receptionist, dto.Password);
            if (!result.Succeeded)
                return Fail("Registration failed.", result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(receptionist, "Receptionist");
            return await BuildAuthResponseAsync(receptionist, "Receptionist registered successfully.");
        }

        public async Task<AuthResponseDto> LoginReceptionistAsync(LoginReceptionistDto dto)
        {
            var receptionist = await _userManager.FindByEmailAsync(dto.Email);
            if (receptionist == null || receptionist.Role != UserRole.Receptionist ||
                !await _userManager.CheckPasswordAsync(receptionist, dto.Password))
                return Fail("Invalid email or password.");

            return await BuildAuthResponseAsync(receptionist, "Login successful.");
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var user = _userManager.Users
                .FirstOrDefault(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Fail("Invalid or expired refresh token.");

            return await BuildAuthResponseAsync(user, "Token refreshed.");
        }

        public async Task RevokeTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;
            user.RefreshToken       = null;
            user.RefreshTokenExpiry = DateTime.MinValue;
            await _userManager.UpdateAsync(user);
        }

        // ── Helpers ───────────────────────────────────────────────────────
        private async Task<AuthResponseDto> BuildAuthResponseAsync(
            ApplicationUser user, string message)
        {
            var roles        = await _userManager.GetRolesAsync(user);
            var accessToken  = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken       = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                IsSuccess    = true,
                Message      = message,
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
                User = new UserInfoDto
                {
                    Id           = user.Id,
                    FirstName    = user.FirstName,
                    LastName     = user.LastName,
                    Email        = user.Email!,
                    Role         = roles.FirstOrDefault() ?? string.Empty,
                    HospitalId   = user.HospitalId,
                    HospitalName = user.Hospital?.Name
                }
            };
        }

        private static AuthResponseDto Fail(string message, IEnumerable<string>? errors = null) =>
            new() { IsSuccess = false, Message = message, Errors = errors };
    }
}

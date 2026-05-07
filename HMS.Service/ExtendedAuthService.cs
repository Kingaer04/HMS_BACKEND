using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Auth;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class ExtendedAuthService : IExtendedAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenService               _tokenService;
        private readonly ApplicationDbContext        _context;

        public ExtendedAuthService(
            UserManager<ApplicationUser> userManager,
            ITokenService tokenService,
            ApplicationDbContext context)
        {
            _userManager  = userManager;
            _tokenService = tokenService;
            _context      = context;
        }

        // ── Lab Technician Registration ───────────────────────────────
        public async Task<AuthResponseDto> RegisterLabTechnicianAsync(
            RegisterLabTechnicianDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Fail("An account with this email already exists.");

            var labTech = new ApplicationUser
            {
                FirstName   = dto.FirstName.Trim(),
                LastName    = dto.LastName.Trim(),
                Email       = dto.Email.Trim(),
                UserName    = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Role        = UserRole.LabTechnician,
                HospitalId  = dto.HospitalId,
            };

            var result = await _userManager.CreateAsync(labTech, dto.Password);
            if (!result.Succeeded)
                return Fail("Registration failed.",
                    result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(labTech, "LabTechnician");

            _context.LabTechnicianProfiles.Add(new LabTechnicianProfile
            {
                Id             = Guid.NewGuid(),
                UserId         = labTech.Id,
                HospitalId     = dto.HospitalId,
                Specialization = dto.Specialization.Trim(),
                LicenseNumber  = dto.LicenseNumber?.Trim(),
                IsActive       = true,
                CreatedAt      = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();

            return await BuildResponseAsync(labTech,
                "Lab technician registered successfully.");
        }

        // ── Lab Technician Login ──────────────────────────────────────
        public async Task<AuthResponseDto> LoginLabTechnicianAsync(
            LoginLabTechnicianDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || user.Role != UserRole.LabTechnician)
                return Fail("Invalid email or password.");

            if (!user.IsActive)
                return Fail("Your account has been deactivated. Contact the hospital admin.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return Fail("Invalid email or password.");

            return await BuildResponseAsync(user, "Login successful.");
        }

        // ── Patient App Registration ──────────────────────────────────
        public async Task<AuthResponseDto> RegisterPatientAppAsync(
            RegisterPatientAppDto dto)
        {
            // 1. Find patient by HMS ID
            var patient = await _context.Patients
                .Include(p => p.OriginHospital)
                .FirstOrDefaultAsync(p =>
                    p.HmsPatientId == dto.HmsPatientId.Trim().ToUpper());

            if (patient == null)
                return Fail(
                    "HMS Patient ID not found. " +
                    "Please register at a hospital first.");

            // 2. Verify DOB matches
            if (patient.DateOfBirth.Date != dto.DateOfBirth.Date)
                return Fail(
                    "Date of birth does not match our records. " +
                    "Please check your details.");

            // 3. Check no existing app account
            if (patient.UserId != null)
                return Fail(
                    "This patient record already has an app account. " +
                    "Please log in instead.");

            // 4. Check email not taken
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return Fail("An account with this email already exists.");

            // 5. Create user account
            var user = new ApplicationUser
            {
                FirstName   = patient.FirstName,
                LastName    = patient.LastName,
                Email       = dto.Email.Trim(),
                UserName    = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                Role        = UserRole.Patient,
                HospitalId  = patient.OriginHospitalId,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return Fail("Registration failed.",
                    result.Errors.Select(e => e.Description));

            await _userManager.AddToRoleAsync(user, "Patient");

            // 6. Link to patient record
            patient.UserId = user.Id;
            patient.Email  = dto.Email.Trim();
            await _context.SaveChangesAsync();

            return await BuildResponseAsync(user,
                $"Welcome {patient.FirstName}! Your patient account is ready.");
        }

        // ── Patient Login ─────────────────────────────────────────────
        public async Task<AuthResponseDto> LoginPatientAsync(LoginPatientDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || user.Role != UserRole.Patient)
                return Fail("Invalid email or password.");

            if (!user.IsActive)
                return Fail("Your account has been deactivated.");

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
                return Fail("Invalid email or password.");

            return await BuildResponseAsync(user, "Login successful.");
        }

        // ── Helpers ───────────────────────────────────────────────────
        private async Task<AuthResponseDto> BuildResponseAsync(
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
                    Id         = user.Id,
                    FirstName  = user.FirstName,
                    LastName   = user.LastName,
                    Email      = user.Email!,
                    Role       = roles.FirstOrDefault() ?? string.Empty,
                    HospitalId = user.HospitalId,
                }
            };
        }

        private static AuthResponseDto Fail(
            string message, IEnumerable<string>? errors = null) =>
            new() { IsSuccess = false, Message = message, Errors = errors };
    }
}

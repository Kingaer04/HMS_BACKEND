using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Profile;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext        _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context     = context;
            _userManager = userManager;
        }

        // ── Doctor ────────────────────────────────────────────────────
        public async Task<ApiResponse<DoctorProfileDto>> GetDoctorProfileAsync(
            string userId)
        {
            var profile = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (profile == null)
                return ApiResponse<DoctorProfileDto>.Failure(
                    "Doctor profile not found.");

            return ApiResponse<DoctorProfileDto>.Success(Map(profile));
        }

        public async Task<ApiResponse<DoctorProfileDto>> UpdateDoctorProfileAsync(
            string userId, UpdateDoctorProfileDto dto)
        {
            var profile = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (profile == null)
                return ApiResponse<DoctorProfileDto>.Failure(
                    "Doctor profile not found.");

            if (dto.Specialization       != null) profile.Specialization       = dto.Specialization;
            if (dto.Qualifications       != null) profile.Qualifications       = dto.Qualifications;
            if (dto.Biography            != null) profile.Biography            = dto.Biography;
            if (dto.YearsOfExperience    != null) profile.YearsOfExperience    = dto.YearsOfExperience.Value;
            if (dto.DepartmentId         != null) profile.DepartmentId         = dto.DepartmentId;
            if (dto.IsAvailableToday     != null) profile.IsAvailableToday     = dto.IsAvailableToday.Value;
            if (dto.MaxPatientsPerDay    != null) profile.MaxPatientsPerDay    = dto.MaxPatientsPerDay.Value;
            if (dto.AvailabilitySchedule != null) profile.AvailabilitySchedule = dto.AvailabilitySchedule;

            await _context.SaveChangesAsync();

            // Reload with fresh includes
            profile = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstAsync(d => d.UserId == userId);

            return ApiResponse<DoctorProfileDto>.Success(
                Map(profile), "Profile updated successfully.");
        }

        public async Task<ApiResponse<string>> ToggleDoctorAvailabilityAsync(
            string userId, bool isAvailable)
        {
            var profile = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (profile == null)
                return ApiResponse<string>.Failure("Doctor profile not found.");

            profile.IsAvailableToday = isAvailable;
            await _context.SaveChangesAsync();

            var status = isAvailable ? "available" : "unavailable";
            return ApiResponse<string>.Success("",
                $"You are now marked as {status} for today.");
        }

        // ── Staff ─────────────────────────────────────────────────────
        public async Task<ApiResponse<StaffProfileDto>> GetStaffProfileAsync(
            string userId)
        {
            var user = await _context.Users
                .Include(u => u.Hospital)
                .Include(u => u.ReceptionistProfile)
                .Include(u => u.LabTechnicianProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ApiResponse<StaffProfileDto>.Failure("User not found.");

            return ApiResponse<StaffProfileDto>.Success(MapStaff(user));
        }

        public async Task<ApiResponse<StaffProfileDto>> UpdateStaffProfileAsync(
            string userId, UpdateStaffProfileDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Hospital)
                .Include(u => u.ReceptionistProfile)
                .Include(u => u.LabTechnicianProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return ApiResponse<StaffProfileDto>.Failure("User not found.");

            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

            if (user.ReceptionistProfile != null && dto.NursingLicenseNumber != null)
                user.ReceptionistProfile.NursingLicenseNumber = dto.NursingLicenseNumber;

            if (user.LabTechnicianProfile != null && dto.Specialization != null)
                user.LabTechnicianProfile.Specialization = dto.Specialization;

            await _context.SaveChangesAsync();

            return ApiResponse<StaffProfileDto>.Success(
                MapStaff(user), "Profile updated.");
        }

        // ── Change Password ───────────────────────────────────────────
        public async Task<ApiResponse<string>> ChangePasswordAsync(
            string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponse<string>.Failure("User not found.");

            var result = await _userManager.ChangePasswordAsync(
                user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return ApiResponse<string>.Failure(
                    "Password change failed.",
                    result.Errors.Select(e => e.Description));

            // Force re-login on all devices
            user.RefreshToken       = null;
            user.RefreshTokenExpiry = DateTime.MinValue;
            await _userManager.UpdateAsync(user);

            return ApiResponse<string>.Success("",
                "Password changed. Please log in again.");
        }

        // ── Mappers ───────────────────────────────────────────────────
        private static DoctorProfileDto Map(DoctorProfile p) => new()
        {
            UserId               = p.UserId,
            FullName             = p.User?.FullName ?? string.Empty,
            Email                = p.User?.Email    ?? string.Empty,
            PhoneNumber          = p.User?.PhoneNumber ?? string.Empty,
            Specialization       = p.Specialization,
            MedicalLicenseNumber = p.MedicalLicenseNumber,
            YearsOfExperience    = p.YearsOfExperience,
            Qualifications       = p.Qualifications,
            Biography            = p.Biography,
            Department           = p.Department?.Name,
            DepartmentId         = p.DepartmentId,
            IsAvailableToday     = p.IsAvailableToday,
            MaxPatientsPerDay    = p.MaxPatientsPerDay,
            CurrentPatientCount  = p.CurrentPatientCount,
            AvailabilitySchedule = p.AvailabilitySchedule,
            IsActive             = p.IsActive,
        };

        private static StaffProfileDto MapStaff(ApplicationUser u) => new()
        {
            UserId        = u.Id,
            FullName      = u.FullName,
            Email         = u.Email ?? string.Empty,
            PhoneNumber   = u.PhoneNumber ?? string.Empty,
            Role          = u.Role.ToString(),
            HospitalName  = u.Hospital?.Name ?? string.Empty,
            IsActive      = u.IsActive,
            CreatedAt     = u.CreatedAt,
            Specialization = u.LabTechnicianProfile?.Specialization,
            LicenseNumber  = u.ReceptionistProfile?.NursingLicenseNumber
                           ?? u.LabTechnicianProfile?.LicenseNumber,
        };
    }
}

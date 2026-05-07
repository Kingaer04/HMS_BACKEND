using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Admin;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext        _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService               _auditService;

        public AdminService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _context      = context;
            _userManager  = userManager;
            _auditService = auditService;
        }

        // ── Dashboard Stats ───────────────────────────────────────────
        public async Task<ApiResponse<HospitalStatsDto>> GetHospitalStatsAsync(
            Guid hospitalId)
        {
            var hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.Id == hospitalId);

            if (hospital == null)
                return ApiResponse<HospitalStatsDto>.Failure("Hospital not found.");

            var today = DateTime.UtcNow.Date;

            // Staff counts grouped by role — single query
            var staffCounts = await _context.Users
                .Where(u => u.HospitalId == hospitalId && u.IsActive)
                .GroupBy(u => u.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            int doctors       = staffCounts.FirstOrDefault(s => s.Role == UserRole.Doctor)?.Count       ?? 0;
            int receptionists = staffCounts.FirstOrDefault(s => s.Role == UserRole.Receptionist)?.Count ?? 0;
            int labTechs      = staffCounts.FirstOrDefault(s => s.Role == UserRole.LabTechnician)?.Count ?? 0;

            // Visits today
            var todayVisits = await _context.HospitalVisits
                .Where(v => v.HospitalId == hospitalId &&
                            v.CheckInTime.Date == today)
                .ToListAsync();

            var activeVisits = todayVisits
                .Count(v => v.Status != VisitStatus.CheckedOut);

            // Pending lab requests
            var pendingLab = await _context.LabRequests
                .CountAsync(l => l.HospitalId == hospitalId &&
                                 l.Status != LabTestStatus.Completed &&
                                 l.Status != LabTestStatus.Cancelled);

            // Pending appointments
            var pendingAppts = await _context.Appointments
                .CountAsync(a => a.HospitalId == hospitalId &&
                                 a.Status == AppointmentStatus.Pending);

            // Registered patients
            var totalPatients = await _context.Patients
                .CountAsync(p => p.OriginHospitalId == hospitalId && p.IsActive);

            // Revenue today
            var todayRevenue = await _context.Payments
                .Where(p => p.HospitalId == hospitalId &&
                            p.PaidAt.HasValue &&
                            p.PaidAt!.Value.Date == today &&
                            p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            // Revenue this month
            var monthStart   = new DateTime(today.Year, today.Month, 1);
            var monthRevenue = await _context.Payments
                .Where(p => p.HospitalId == hospitalId &&
                            p.PaidAt.HasValue &&
                            p.PaidAt!.Value >= monthStart &&
                            p.Status == PaymentStatus.Paid)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            return ApiResponse<HospitalStatsDto>.Success(new HospitalStatsDto
            {
                HospitalName            = hospital.Name,
                HospitalUID             = hospital.HospitalUID,
                State                   = hospital.State,
                TotalDoctors            = doctors,
                TotalReceptionists      = receptionists,
                TotalLabTechnicians     = labTechs,
                TotalStaff              = doctors + receptionists + labTechs,
                TotalRegisteredPatients = totalPatients,
                TodayVisits             = todayVisits.Count,
                ActiveVisits            = activeVisits,
                PendingLabRequests      = pendingLab,
                PendingAppointments     = pendingAppts,
                TodayRevenue            = todayRevenue,
                MonthRevenue            = monthRevenue,
            });
        }

        // ── Staff List ────────────────────────────────────────────────
        public async Task<ApiResponse<List<StaffListItemDto>>> GetStaffListAsync(
            Guid hospitalId, string? role)
        {
            var query = _context.Users
                .Include(u => u.DoctorProfile)
                .Include(u => u.LabTechnicianProfile)
                .Where(u => u.HospitalId == hospitalId &&
                            u.Role != UserRole.Patient);

            if (!string.IsNullOrEmpty(role) &&
                Enum.TryParse<UserRole>(role, true, out var parsedRole))
                query = query.Where(u => u.Role == parsedRole);

            var staff = await query
                .OrderBy(u => u.Role)
                .ThenBy(u => u.LastName)
                .Select(u => new StaffListItemDto
                {
                    UserId         = u.Id,
                    FullName       = u.FullName,
                    Email          = u.Email ?? string.Empty,
                    PhoneNumber    = u.PhoneNumber ?? string.Empty,
                    Role           = u.Role.ToString(),
                    Specialization = u.DoctorProfile != null
                        ? u.DoctorProfile.Specialization
                        : u.LabTechnicianProfile != null
                            ? u.LabTechnicianProfile.Specialization
                            : null,
                    IsActive  = u.IsActive,
                    CreatedAt = u.CreatedAt,
                })
                .ToListAsync();

            return ApiResponse<List<StaffListItemDto>>.Success(staff);
        }

        // ── Deactivate ────────────────────────────────────────────────
        public async Task<ApiResponse<string>> DeactivateStaffAsync(
            string staffUserId, string adminUserId, string reason)
        {
            var staff = await _userManager.FindByIdAsync(staffUserId);
            if (staff == null)
                return ApiResponse<string>.Failure("Staff member not found.");

            if (staff.Role == UserRole.HospitalAdmin)
                return ApiResponse<string>.Failure(
                    "Cannot deactivate a hospital admin.");

            if (!staff.IsActive)
                return ApiResponse<string>.Failure(
                    "Staff member is already deactivated.");

            staff.IsActive     = false;
            staff.RefreshToken = null; // Force logout immediately
            await _userManager.UpdateAsync(staff);

            // Deactivate role profile
            var doctor = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == staffUserId);
            if (doctor != null) doctor.IsActive = false;

            var labTech = await _context.LabTechnicianProfiles
                .FirstOrDefaultAsync(l => l.UserId == staffUserId);
            if (labTech != null) labTech.IsActive = false;

            var receptionist = await _context.ReceptionistProfiles
                .FirstOrDefaultAsync(r => r.UserId == staffUserId);
            if (receptionist != null) receptionist.IsActive = false;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                adminUserId, "", "HospitalAdmin",
                "DEACTIVATE_STAFF", "ApplicationUser", staffUserId,
                hospitalId: staff.HospitalId);

            return ApiResponse<string>.Success("",
                $"{staff.FullName} has been deactivated.");
        }

        // ── Reactivate ────────────────────────────────────────────────
        public async Task<ApiResponse<string>> ReactivateStaffAsync(
            string staffUserId, string adminUserId)
        {
            var staff = await _userManager.FindByIdAsync(staffUserId);
            if (staff == null)
                return ApiResponse<string>.Failure("Staff member not found.");

            if (staff.IsActive)
                return ApiResponse<string>.Failure(
                    "Staff member is already active.");

            staff.IsActive = true;
            await _userManager.UpdateAsync(staff);

            var doctor = await _context.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == staffUserId);
            if (doctor != null) doctor.IsActive = true;

            var labTech = await _context.LabTechnicianProfiles
                .FirstOrDefaultAsync(l => l.UserId == staffUserId);
            if (labTech != null) labTech.IsActive = true;

            var receptionist = await _context.ReceptionistProfiles
                .FirstOrDefaultAsync(r => r.UserId == staffUserId);
            if (receptionist != null) receptionist.IsActive = true;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                adminUserId, "", "HospitalAdmin",
                "REACTIVATE_STAFF", "ApplicationUser", staffUserId,
                hospitalId: staff.HospitalId);

            return ApiResponse<string>.Success("",
                $"{staff.FullName} has been reactivated.");
        }
    }
}

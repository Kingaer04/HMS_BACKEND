using System.ComponentModel.DataAnnotations;

namespace HMS.Shared.DTOs.Profile
{
    public class UpdateDoctorProfileDto
    {
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
        public int? YearsOfExperience { get; set; }
        public string? Biography { get; set; }
        public Guid? DepartmentId { get; set; }
        public bool? IsAvailableToday { get; set; }
        public int? MaxPatientsPerDay { get; set; }
        public string? AvailabilitySchedule { get; set; }
    }

    public class DoctorProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string MedicalLicenseNumber { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public string? Qualifications { get; set; }
        public string? Biography { get; set; }
        public string? Department { get; set; }
        public Guid? DepartmentId { get; set; }
        public bool IsAvailableToday { get; set; }
        public int MaxPatientsPerDay { get; set; }
        public int CurrentPatientCount { get; set; }
        public string? AvailabilitySchedule { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateStaffProfileDto
    {
        [Phone] public string? PhoneNumber { get; set; }
        public string? NursingLicenseNumber { get; set; }
        public string? Specialization { get; set; }
    }

    public class StaffProfileDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public string? LicenseNumber { get; set; }
        public string HospitalName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required][MinLength(8)] public string NewPassword { get; set; } = string.Empty;
        [Required][Compare("NewPassword")] public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}

namespace HMS.Shared.DTOs.Admin
{
    public class HospitalStatsDto
    {
        public string HospitalName { get; set; } = string.Empty;
        public string HospitalUID { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int TotalDoctors { get; set; }
        public int TotalReceptionists { get; set; }
        public int TotalLabTechnicians { get; set; }
        public int TotalStaff { get; set; }
        public int TotalRegisteredPatients { get; set; }
        public int TodayVisits { get; set; }
        public int ActiveVisits { get; set; }
        public int PendingLabRequests { get; set; }
        public int PendingAppointments { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
    }

    public class StaffListItemDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeactivateStaffDto
    {
        [Required] public string Reason { get; set; } = string.Empty;
    }
}

using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Visit;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<DepartmentDto>>> GetDepartmentsAsync(Guid hospitalId)
        {
            var departments = await _context.Departments
                .Include(d => d.Doctors)
                .Where(d => d.HospitalId == hospitalId && d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto
                {
                    Id          = d.Id,
                    Name        = d.Name,
                    Description = d.Description,
                    DoctorCount = d.Doctors.Count(doc => doc.IsActive)
                })
                .ToListAsync();

            return ApiResponse<List<DepartmentDto>>.Success(departments);
        }

        public async Task<ApiResponse<DepartmentDto>> CreateDepartmentAsync(
            Guid hospitalId, string name, string? description)
        {
            var exists = await _context.Departments
                .AnyAsync(d => d.HospitalId == hospitalId &&
                               d.Name.ToLower() == name.ToLower());

            if (exists)
                return ApiResponse<DepartmentDto>.Failure(
                    $"Department '{name}' already exists.");

            var dept = new Department
            {
                Id          = Guid.NewGuid(),
                HospitalId  = hospitalId,
                Name        = name.Trim(),
                Description = description,
                IsActive    = true,
            };

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            return ApiResponse<DepartmentDto>.Success(new DepartmentDto
            {
                Id          = dept.Id,
                Name        = dept.Name,
                Description = dept.Description,
                DoctorCount = 0
            }, "Department created.");
        }

        public async Task<ApiResponse<List<DoctorSummaryDto>>> GetDoctorsByDepartmentAsync(
            Guid departmentId)
        {
            var doctors = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .Where(d => d.DepartmentId == departmentId && d.IsActive)
                .Select(d => new DoctorSummaryDto
                {
                    UserId              = d.UserId,
                    FullName            = d.User!.FullName,
                    Specialization      = d.Specialization,
                    Department          = d.Department!.Name,
                    IsAvailableToday    = d.IsAvailableToday,
                    CurrentPatientCount = d.CurrentPatientCount,
                    MaxPatientsPerDay   = d.MaxPatientsPerDay,
                })
                .ToListAsync();

            return ApiResponse<List<DoctorSummaryDto>>.Success(doctors);
        }

        public async Task<ApiResponse<List<DoctorSummaryDto>>> GetAvailableDoctorsAsync(
            Guid hospitalId)
        {
            var doctors = await _context.DoctorProfiles
                .Include(d => d.User)
                .Include(d => d.Department)
                .Where(d => d.HospitalId == hospitalId &&
                            d.IsActive &&
                            d.IsAvailableToday &&
                            d.CurrentPatientCount < d.MaxPatientsPerDay)
                .OrderBy(d => d.CurrentPatientCount)
                .Select(d => new DoctorSummaryDto
                {
                    UserId              = d.UserId,
                    FullName            = d.User!.FullName,
                    Specialization      = d.Specialization,
                    Department          = d.Department != null ? d.Department.Name : null,
                    IsAvailableToday    = d.IsAvailableToday,
                    CurrentPatientCount = d.CurrentPatientCount,
                    MaxPatientsPerDay   = d.MaxPatientsPerDay,
                })
                .ToListAsync();

            return ApiResponse<List<DoctorSummaryDto>>.Success(doctors);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace HMS.Shared.DTOs.Auth
{
    public class RegisterLabTechnicianDto
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)] public string LastName { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string Specialization { get; set; } = string.Empty;
        public string? LicenseNumber { get; set; }
        [Required][MinLength(8)] public string Password { get; set; } = string.Empty;
        [Required][Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
    }

    public class LoginLabTechnicianDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class RegisterPatientAppDto
    {
        [Required(ErrorMessage = "HMS Patient ID is required.")]
        public string HmsPatientId { get; set; } = string.Empty;
        [Required][Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][MinLength(8)] public string Password { get; set; } = string.Empty;
        [Required][Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Date of birth is required for verification.")]
        public DateTime DateOfBirth { get; set; }
    }

    public class LoginPatientDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }
}

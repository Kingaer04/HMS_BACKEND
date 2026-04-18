using System.ComponentModel.DataAnnotations;

namespace HMS.Shared.DTOs.Auth
{
    public class RegisterDoctorDto
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required][Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string MedicalLicenseNumber { get; set; } = string.Empty;
        [Required][MinLength(8)] public string Password { get; set; } = string.Empty;
        [Required][Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;

        // Injected from admin token — not from client
        public Guid HospitalId { get; set; }
    }

    public class LoginDoctorDto
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }
}

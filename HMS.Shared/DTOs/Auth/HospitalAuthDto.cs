using System.ComponentModel.DataAnnotations;

namespace HMS.Shared.DTOs.Auth
{
    public class VerifyHospitalUIDDto
    {
        [Required(ErrorMessage = "Hospital UID is required.")]
        public string HospitalUID { get; set; } = string.Empty;
    }

    public class RegisterHospitalDto
    {
        [Required] public string HospitalName { get; set; } = string.Empty;
        [Required] public string HospitalUID { get; set; } = string.Empty;

        // Address
        [Required] public string BlockNumber { get; set; } = string.Empty;
        [Required] public string Street { get; set; } = string.Empty;
        [Required] public string State { get; set; } = string.Empty;

        // Contact
        [Required][Phone] public string PhoneNumber { get; set; } = string.Empty;
        [Phone] public string? AlternativePhoneNumber { get; set; }
        [Required][EmailAddress] public string HospitalEmail { get; set; } = string.Empty;

        // Admin (Representative)
        [Required] public string AdminFirstName { get; set; } = string.Empty;
        [Required] public string AdminLastName { get; set; } = string.Empty;
        [Required][EmailAddress] public string AdminEmail { get; set; } = string.Empty;
        [Required][Phone] public string AdminPhoneNumber { get; set; } = string.Empty;

        // Password
        [Required][MinLength(8)] public string Password { get; set; } = string.Empty;
        [Required][Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginHospitalDto
    {
        [Required(ErrorMessage = "Hospital UID is required.")]
        public string HospitalUID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}

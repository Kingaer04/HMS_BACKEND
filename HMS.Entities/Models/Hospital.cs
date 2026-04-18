namespace HMS.Entities.Models
{
    public class Hospital
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string HospitalUID { get; set; } = string.Empty;

        // Address
        public string BlockNumber { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = "Nigeria";

        // Contact
        public string PhoneNumber { get; set; } = string.Empty;
        public string? AlternativePhoneNumber { get; set; }
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<ApplicationUser> Staff { get; set; } = new List<ApplicationUser>();
    }
}

namespace HMS.Entities.Models
{
    public class NhisVerificationResult
    {
        public bool IsValid { get; set; }
        public string? HospitalName { get; set; }
        public string? Message { get; set; }
    }
}

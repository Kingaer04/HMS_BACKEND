using HMS.Entities.Models;
using HMS.Service.Contracts;

namespace HMS.Service
{
    /// <summary>
    /// MOCK NHIS verification — replace VerifyAsync body with real HTTP call when ready.
    /// The interface and all callers stay unchanged.
    /// </summary>
    public class NhisVerificationService : INhisVerificationService
    {
        private static readonly Dictionary<string, string> _mockRegistry = new()
        {
            { "NHIS-0001-LG", "Lagos General Hospital" },
            { "NHIS-0002-AB", "Abuja National Hospital" },
            { "NHIS-0003-KN", "Kano State Hospital" },
            { "NHIS-0004-PH", "Port Harcourt Teaching Hospital" },
            { "NHIS-0005-IB", "University College Hospital Ibadan" },
        };

        public Task<NhisVerificationResult> VerifyAsync(string hospitalUID)
        {
            var uid = hospitalUID.Trim().ToUpperInvariant();

            if (_mockRegistry.TryGetValue(uid, out var name))
                return Task.FromResult(new NhisVerificationResult
                {
                    IsValid      = true,
                    HospitalName = name,
                    Message      = "Hospital UID verified successfully."
                });

            return Task.FromResult(new NhisVerificationResult
            {
                IsValid = false,
                Message = "Hospital UID not found in the NHIS registry."
            });

            // TODO: swap with real API
            // var response = await _httpClient.GetAsync($"https://api.nhis.gov.ng/verify/{uid}");
            // var result   = await response.Content.ReadFromJsonAsync<NhisApiResponse>();
            // return new NhisVerificationResult { IsValid = result.IsValid, ... };
        }
    }
}

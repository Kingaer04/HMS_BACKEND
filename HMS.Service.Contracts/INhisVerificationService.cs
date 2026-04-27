using HMS.Entities.Models;

namespace HMS.Service.Contracts
{
    public interface INhisVerificationService
    {
        /// <summary>
        /// Verifies if the Hospital UID exists and is valid with NHIS.
        /// Currently uses mock data — swap implementation for real API later.
        /// </summary>
        Task<NhisVerificationResult> VerifyAsync(string hospitalUID);
    }
}

namespace HMS.Entities.Models
{
    /// <summary>
    /// Tracks the sequence counter used to generate HmsPatientIds.
    /// One row per year — sequence resets each year.
    /// e.g. 2025 → HMS-2025-000001, HMS-2025-000002 ...
    ///      2026 → HMS-2026-000001 (fresh sequence)
    /// </summary>
    public class PatientIdSequence
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int LastSequenceNumber { get; set; } = 0;
    }
}

namespace HMS.Entities.Models
{
    /// <summary>
    /// System-wide audit trail — every significant action is logged here.
    /// Essential for HIPAA-style compliance and dispute resolution.
    /// </summary>
    public class AuditLog
    {
        public Guid Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public Guid? HospitalId { get; set; }

        public string Action { get; set; } = string.Empty;      // e.g. "UPDATE_VITALS"
        public string EntityType { get; set; } = string.Empty;  // e.g. "VitalInfo"
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }                   // JSON snapshot before
        public string? NewValues { get; set; }                   // JSON snapshot after
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Created when an emergency patient cannot be identified.
    /// Stores all collected info until the patient can be identified
    /// and the record merged with their existing record.
    /// </summary>
    public class TemporaryPatientRecord
    {
        public Guid Id { get; set; }
        public Guid VisitId { get; set; }
        public HospitalVisit? Visit { get; set; }

        public Guid HospitalId { get; set; }

        // Partial info collected at scene / on arrival
        public string? EstimatedFirstName { get; set; }
        public string? EstimatedLastName { get; set; }
        public string? EstimatedAge { get; set; }
        public string? EstimatedGender { get; set; }
        public string? PhysicalDescription { get; set; }
        public string? ItemsFoundOnPatient { get; set; }  // IDs, phone, etc.

        // If someone accompanies the patient
        public string? CompanionName { get; set; }
        public string? CompanionPhone { get; set; }
        public string? CompanionRelationship { get; set; }

        public string CreatedByReceptionistId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Merge tracking
        public bool IsMerged { get; set; } = false;
        public Guid? MergedIntoPatientId { get; set; }
        public DateTime? MergedAt { get; set; }
        public string? MergedByUserId { get; set; }
    }
}

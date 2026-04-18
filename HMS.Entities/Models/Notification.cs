using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// System notification — sent in real-time via SignalR.
    /// Also persisted so users can see missed notifications on login.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }

        // ── Recipient ─────────────────────────────────────────────────
        public string RecipientUserId { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }

        // ── Content ───────────────────────────────────────────────────
        public NotificationType Type { get; set; }
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// JSON payload — extra data attached to the notification.
        /// e.g. for PatientAssigned: { patientId, patientName, chiefComplaint, visitId }
        /// </summary>
        public string? Payload { get; set; }

        // ── Related Entities ──────────────────────────────────────────
        public Guid? RelatedPatientId { get; set; }
        public Guid? RelatedVisitId { get; set; }
        public Guid? RelatedAppointmentId { get; set; }
        public HospitalVisit? RelatedVisit { get; set; }
        public Appointment? RelatedAppointment { get; set; }

        // ── Timestamps ────────────────────────────────────────────────
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Whether this was delivered via SignalR in real-time.
        /// False means the user was offline — they'll see it on next login.
        /// </summary>
        public bool WasDeliveredRealTime { get; set; } = false;
    }
}

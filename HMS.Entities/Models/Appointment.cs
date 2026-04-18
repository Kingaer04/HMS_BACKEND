using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// A patient-requested appointment — can come from the patient app
    /// or be created by reception. Requires doctor + receptionist approval.
    /// </summary>
    public class Appointment
    {
        public Guid Id { get; set; }

        // ── Parties ───────────────────────────────────────────────────
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        /// <summary>
        /// Preferred doctor — patient can specify or leave null for any available.
        /// </summary>
        public string? PreferredDoctorId { get; set; }
        public string? PreferredDoctorName { get; set; }

        /// <summary>
        /// Doctor confirmed to handle the appointment after approval.
        /// </summary>
        public string? AssignedDoctorId { get; set; }
        public string? AssignedDoctorName { get; set; }

        // ── Scheduling ────────────────────────────────────────────────
        public DateTime RequestedDateTime { get; set; }
        public DateTime? ConfirmedDateTime { get; set; }
        public AppointmentType Type { get; set; } = AppointmentType.Scheduled;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public string? ReasonForAppointment { get; set; }

        // ── Approval Flow ─────────────────────────────────────────────
        // Step 1 — Receptionist reviews patient request
        public string? ReviewedByReceptionistId { get; set; }
        public DateTime? ReviewedByReceptionistAt { get; set; }
        public bool? ReceptionistApproved { get; set; }
        public string? ReceptionistNote { get; set; }

        // Step 2 — Doctor confirms they can take the appointment
        public DateTime? DoctorRespondedAt { get; set; }
        public bool? DoctorApproved { get; set; }
        public string? DoctorNote { get; set; }

        // ── Rejection ─────────────────────────────────────────────────
        public string? RejectionReason { get; set; }
        public string? RejectedBy { get; set; }

        // ── Outcome ───────────────────────────────────────────────────
        public Guid? ResultingVisitId { get; set; }
        public HospitalVisit? ResultingVisit { get; set; }

        // ── Source ────────────────────────────────────────────────────
        /// <summary>True if booked via patient app, false if walk-in or reception-created.</summary>
        public bool BookedViaApp { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        // ── Navigation ────────────────────────────────────────────────
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}

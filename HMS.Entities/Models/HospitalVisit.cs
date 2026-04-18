using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// Represents a single visit by a patient to a hospital.
    /// Tracks the full lifecycle: check-in → doctor assigned → consultation → checkout.
    /// One patient can have many visits across different hospitals.
    /// </summary>
    public class HospitalVisit
    {
        public Guid Id { get; set; }

        // ── Who and Where ─────────────────────────────────────────────
        public Guid PatientId { get; set; }
        public Patient? Patient { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        // ── Visit Type & Status ───────────────────────────────────────
        public AppointmentType VisitType { get; set; } = AppointmentType.WalkIn;
        public VisitStatus Status { get; set; } = VisitStatus.CheckedIn;

        /// <summary>
        /// If this visit originated from a scheduled appointment.
        /// </summary>
        public Guid? AppointmentId { get; set; }
        public Appointment? Appointment { get; set; }

        // ── Check-in ──────────────────────────────────────────────────
        public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
        public string CheckedInByReceptionistId { get; set; } = string.Empty;
        public string CheckedInByReceptionistName { get; set; } = string.Empty;
        public string? ChiefComplaintOnArrival { get; set; }    // What patient said at reception
        public bool IsEmergency { get; set; } = false;

        // ── Doctor Assignment ─────────────────────────────────────────
        public string? AssignedDoctorId { get; set; }
        public string? AssignedDoctorName { get; set; }
        public DateTime? DoctorAssignedAt { get; set; }
        public DateTime? DoctorNotifiedAt { get; set; }
        public DateTime? DoctorGrantedEntryAt { get; set; }
        public DateTime? ConsultationStartedAt { get; set; }
        public DateTime? ConsultationEndedAt { get; set; }

        // ── Lab Referral ──────────────────────────────────────────────
        public bool LabReferralMade { get; set; } = false;
        public DateTime? LabReferralAt { get; set; }

        // ── Checkout ──────────────────────────────────────────────────
        public DateTime? CheckOutTime { get; set; }
        public string? CheckedOutByReceptionistId { get; set; }
        public string? DischargeNotes { get; set; }
        public string? FollowUpDate { get; set; }

        // ── Payment ───────────────────────────────────────────────────
        public Payment? Payment { get; set; }

        // ── Temporary Record Handling ─────────────────────────────────
        /// <summary>
        /// True if patient was unconscious/unidentified on arrival.
        /// A temporary MedicalRecord is created and merged later.
        /// </summary>
        public bool WasTemporaryRecord { get; set; } = false;
        public DateTime? TemporaryRecordMergedAt { get; set; }

        // ── Navigation ────────────────────────────────────────────────
        public ICollection<DoctorNote> DoctorNotes { get; set; } = new List<DoctorNote>();
        public ICollection<VitalReading> VitalReadings { get; set; } = new List<VitalReading>();
        public ICollection<LabRequest> LabRequests { get; set; } = new List<LabRequest>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}

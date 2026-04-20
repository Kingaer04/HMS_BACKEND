namespace HMS.Entities.Enums
{
    public enum AccessLevel
    {
        Open,       // No lock — any authorized staff can view
        Locked      // Requires access code or biometric to unlock
    }

    public enum UnlockMethod
    {
        AccessCode,
        Fingerprint
    }

    // ── Record Types 
    public enum RecordStatus
    {
        Active,
        Temporary,    // Emergency — no identity confirmed yet
        Merged,       // Temp record merged into real record
        Inactive
    }

    // ── Visit / Appointment 
    public enum VisitStatus
    {
        CheckedIn,
        DoctorNotified,
        DoctorGranted,
        InConsultation,
        LabReferred,
        CheckedOut,
        Emergency
    }

    public enum AppointmentStatus
    {
        Pending,           // Patient requested
        AwaitingDoctor,    // Receptionist approved, waiting for doctor
        Confirmed,         // Doctor approved
        Rejected,
        Completed,
        Cancelled,
        NoShow
    }

    public enum AppointmentType
    {
        WalkIn,
        Scheduled,        // Patient booked via app
        Emergency,
        FollowUp,
        LabOnly
    }

    // ── Notification 
    public enum NotificationType
    {
        PatientAssigned,        // Doctor gets this on patient check-in
        AppointmentRequest,     // Receptionist gets this from patient app
        AppointmentConfirmed,   // Patient gets this when doctor approves
        AppointmentRejected,
        LabResultReady,
        PatientCheckedOut,
        EmergencyAlert,
        AccessCodeRequested,
        SystemAlert
    }

    public enum NotificationStatus
    {
        Unread,
        Read,
        Dismissed
    }

    // ── Payment 
    public enum PaymentStatus
    {
        Pending,
        Paid,
        Waived,       // Emergency or exemption
        Partial,
        Refunded
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        BankTransfer,
        Insurance,
        NHIS           // National Health Insurance Scheme
    }

    // ── Lab 
    public enum LabTestStatus
    {
        Requested,
        SampleCollected,
        Processing,
        Completed,
        Cancelled
    }

    public enum BloodGroup
    {
        APositive,
        ANegative,
        BPositive,
        BNegative,
        ABPositive,
        ABNegative,
        OPositive,
        ONegative,
        Unknown
    }

    public enum Gender
    {
        Male,
        Female,
        Other,
        PreferNotToSay
    }

    public enum MaritalStatus
    {
        Single,
        Married,
        Divorced,
        Widowed,
        Other
    }
}

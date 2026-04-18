using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// A lab test ordered by a doctor during a visit.
    /// Lab technician updates the result — doctor reads it.
    /// </summary>
    public class LabRequest
    {
        public Guid Id { get; set; }
        public Guid MedicalRecordId { get; set; }
        public MedicalRecord? MedicalRecord { get; set; }

        public Guid VisitId { get; set; }
        public HospitalVisit? Visit { get; set; }

        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        // ── Ordered by Doctor ─────────────────────────────────────────
        public string RequestedByDoctorId { get; set; } = string.Empty;
        public string RequestedByDoctorName { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string? ClinicalNotes { get; set; }  // Why doctor ordered this test

        public LabTestStatus Status { get; set; } = LabTestStatus.Requested;

        // ── Updated by Lab Technician ─────────────────────────────────
        public string? ProcessedByLabTechId { get; set; }
        public string? ProcessedByLabTechName { get; set; }
        public DateTime? SampleCollectedAt { get; set; }
        public DateTime? ResultReadyAt { get; set; }

        // Navigation to individual test results
        public ICollection<LabTestResult> TestResults { get; set; } = new List<LabTestResult>();
    }

    /// <summary>
    /// Individual test result within a lab request.
    /// A single request can have multiple tests (e.g. Full Blood Count has many parameters).
    /// </summary>
    public class LabTestResult
    {
        public Guid Id { get; set; }
        public Guid LabRequestId { get; set; }
        public LabRequest? LabRequest { get; set; }

        public string TestName { get; set; } = string.Empty;        // e.g. "Haemoglobin"
        public string TestCode { get; set; } = string.Empty;        // e.g. "HGB"
        public string? Result { get; set; }                          // e.g. "12.5"
        public string? Unit { get; set; }                            // e.g. "g/dL"
        public string? ReferenceRange { get; set; }                  // e.g. "12.0 - 17.5"
        public bool? IsAbnormal { get; set; }                        // Flagged by lab tech
        public string? Notes { get; set; }                           // Lab tech comments

        public string? EnteredByLabTechId { get; set; }
        public DateTime? EnteredAt { get; set; }
    }

    /// <summary>
    /// Catalogue of available lab tests at a hospital.
    /// Admin configures this per hospital.
    /// </summary>
    public class LabTestCatalogue
    {
        public Guid Id { get; set; }
        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        public string TestName { get; set; } = string.Empty;
        public string TestCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;  // Haematology, Chemistry, etc.
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ExpectedTurnaroundTime { get; set; }    // e.g. "2 hours"
    }
}

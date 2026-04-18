using HMS.Entities.Enums;

namespace HMS.Entities.Models
{
    /// <summary>
    /// Payment record for a hospital visit.
    /// Created at checkout — itemized so patient can see what they paid for.
    /// </summary>
    public class Payment
    {
        public Guid Id { get; set; }

        public Guid VisitId { get; set; }
        public HospitalVisit? Visit { get; set; }

        public Guid PatientId { get; set; }
        public Guid HospitalId { get; set; }
        public Hospital? Hospital { get; set; }

        // ── Amounts ───────────────────────────────────────────────────
        public decimal ConsultationFee { get; set; }
        public decimal LabFees { get; set; }
        public decimal MedicationFees { get; set; }
        public decimal OtherFees { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal NHISCoverage { get; set; } = 0;   // Amount covered by NHIS

        /// <summary>Calculated: Consultation + Lab + Medication + Other - Discount - NHIS</summary>
        public decimal TotalAmount => ConsultationFee + LabFees + MedicationFees + OtherFees
                                      - Discount - NHISCoverage;

        public decimal AmountPaid { get; set; }
        public decimal Balance => TotalAmount - AmountPaid;

        // ── Payment Details ───────────────────────────────────────────
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public PaymentMethod Method { get; set; }
        public string? TransactionReference { get; set; }
        public string? InsurancePolicyNumber { get; set; }
        public string? NHISNumber { get; set; }

        public string? Notes { get; set; }
        public string? WaiverReason { get; set; }   // If waived — why

        // ── Processed by ─────────────────────────────────────────────
        public string ProcessedByReceptionistId { get; set; } = string.Empty;
        public string ProcessedByReceptionistName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // ── Line Items ────────────────────────────────────────────────
        public ICollection<PaymentLineItem> LineItems { get; set; } = new List<PaymentLineItem>();
    }

    /// <summary>
    /// Individual line items on a payment receipt.
    /// e.g. "Full Blood Count — ₦3,500" or "Consultation — ₦5,000"
    /// </summary>
    public class PaymentLineItem
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public Payment? Payment { get; set; }

        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;   // Consultation, Lab, Medication
        public decimal Amount { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal Total => Amount * Quantity;
    }
}

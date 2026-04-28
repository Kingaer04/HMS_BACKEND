using HMS.Entities.Enums;
using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Payment;
using HMS.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService        _auditService;

        public PaymentService(
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _context      = context;
            _auditService = auditService;
        }

        // ── Create payment record at checkout ─────────────────────────
        public async Task<ApiResponse<PaymentDto>> CreatePaymentAsync(
            Guid visitId, CreatePaymentDto dto,
            string receptionistId, string receptionistName)
        {
            var visit = await _context.HospitalVisits
                .Include(v => v.Payment)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            if (visit == null)
                return ApiResponse<PaymentDto>.Failure("Visit not found.");

            if (visit.Payment != null)
                return ApiResponse<PaymentDto>.Failure(
                    "Payment already exists for this visit.");

            var payment = new Payment
            {
                Id                          = Guid.NewGuid(),
                VisitId                     = visitId,
                PatientId                   = visit.PatientId,
                HospitalId                  = visit.HospitalId,
                ConsultationFee             = dto.ConsultationFee,
                LabFees                     = dto.LabFees,
                MedicationFees              = dto.MedicationFees,
                OtherFees                   = dto.OtherFees,
                Discount                    = dto.Discount,
                NHISCoverage                = dto.NHISCoverage,
                AmountPaid                  = 0,
                Status                      = PaymentStatus.Pending,
                Notes                       = dto.Notes,
                ProcessedByReceptionistId   = receptionistId,
                ProcessedByReceptionistName = receptionistName,
                CreatedAt                   = DateTime.UtcNow,
                LineItems                   = dto.LineItems.Select(l => new PaymentLineItem
                {
                    Id          = Guid.NewGuid(),
                    Description = l.Description,
                    Category    = l.Category,
                    Amount      = l.Amount,
                    Quantity    = l.Quantity,
                }).ToList()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                receptionistId, receptionistName, "Receptionist",
                "CREATE_PAYMENT", "Payment", payment.Id.ToString(),
                hospitalId: visit.HospitalId);

            return ApiResponse<PaymentDto>.Success(
                MapToDto(payment), "Payment record created.");
        }

        // ── Process actual payment ────────────────────────────────────
        public async Task<ApiResponse<PaymentDto>> ProcessPaymentAsync(
            Guid paymentId, ProcessPaymentDto dto, string receptionistId)
        {
            var payment = await _context.Payments
                .Include(p => p.LineItems)
                .Include(p => p.Visit)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return ApiResponse<PaymentDto>.Failure("Payment not found.");

            if (payment.Status == PaymentStatus.Paid)
                return ApiResponse<PaymentDto>.Failure("Payment already completed.");

            var totalAmount = payment.ConsultationFee + payment.LabFees +
                              payment.MedicationFees  + payment.OtherFees -
                              payment.Discount        - payment.NHISCoverage;

            payment.Method               = dto.Method;
            payment.AmountPaid           = dto.AmountPaid;
            payment.TransactionReference = dto.TransactionReference;
            payment.InsurancePolicyNumber = dto.InsurancePolicyNumber;
            payment.NHISNumber           = dto.NHISNumber;
            payment.PaidAt               = DateTime.UtcNow;

            // Determine payment status
            payment.Status = dto.AmountPaid >= totalAmount
                ? PaymentStatus.Paid
                : PaymentStatus.Partial;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                receptionistId, "", "Receptionist",
                "PROCESS_PAYMENT", "Payment", payment.Id.ToString(),
                hospitalId: payment.HospitalId);

            return ApiResponse<PaymentDto>.Success(
                MapToDto(payment),
                payment.Status == PaymentStatus.Paid
                    ? "Payment completed successfully."
                    : $"Partial payment recorded. Balance: ₦{totalAmount - dto.AmountPaid:N2}");
        }

        // ── Get payment by visit ──────────────────────────────────────
        public async Task<ApiResponse<PaymentDto>> GetPaymentByVisitAsync(Guid visitId)
        {
            var payment = await _context.Payments
                .Include(p => p.LineItems)
                .FirstOrDefaultAsync(p => p.VisitId == visitId);

            if (payment == null)
                return ApiResponse<PaymentDto>.Failure("No payment found for this visit.");

            return ApiResponse<PaymentDto>.Success(MapToDto(payment));
        }

        // ── Waive payment — HospitalAdmin only ────────────────────────
        public async Task<ApiResponse<PaymentDto>> WaivePaymentAsync(
            Guid paymentId, string reason, string adminUserId)
        {
            var payment = await _context.Payments
                .Include(p => p.LineItems)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return ApiResponse<PaymentDto>.Failure("Payment not found.");

            payment.Status       = PaymentStatus.Waived;
            payment.WaiverReason = reason;
            payment.AmountPaid   = 0;
            payment.PaidAt       = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                adminUserId, "", "HospitalAdmin",
                "WAIVE_PAYMENT", "Payment", payment.Id.ToString(),
                hospitalId: payment.HospitalId);

            return ApiResponse<PaymentDto>.Success(
                MapToDto(payment), "Payment waived successfully.");
        }

        // ── Helper ────────────────────────────────────────────────────
        private static PaymentDto MapToDto(Payment p)
        {
            var total = p.ConsultationFee + p.LabFees + p.MedicationFees +
                        p.OtherFees - p.Discount - p.NHISCoverage;

            return new PaymentDto
            {
                Id                  = p.Id,
                VisitId             = p.VisitId,
                ConsultationFee     = p.ConsultationFee,
                LabFees             = p.LabFees,
                MedicationFees      = p.MedicationFees,
                OtherFees           = p.OtherFees,
                Discount            = p.Discount,
                NHISCoverage        = p.NHISCoverage,
                TotalAmount         = total,
                AmountPaid          = p.AmountPaid,
                Balance             = total - p.AmountPaid,
                Status              = p.Status.ToString(),
                Method              = p.Method.ToString(),
                TransactionReference = p.TransactionReference,
                ProcessedByName     = p.ProcessedByReceptionistName,
                CreatedAt           = p.CreatedAt,
                PaidAt              = p.PaidAt,
                LineItems           = p.LineItems.Select(l => new PaymentLineItemDto
                {
                    Description = l.Description,
                    Category    = l.Category,
                    Amount      = l.Amount,
                    Quantity    = l.Quantity,
                    Total       = l.Amount * l.Quantity,
                }).ToList()
            };
        }
    }
}

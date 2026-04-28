using HMS.Service.Contracts;
using HMS.Shared.DTOs.Payment;
using HMS.Shared.DTOs.Visit;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Payment processing — create invoice, process payment, waive fees.
    /// Handles cash, card, bank transfer, insurance and NHIS coverage.
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    [Authorize]
    [Produces("application/json")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private string UserId   => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string UserName => $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();

        // ── Create payment invoice ────────────────────────────────────
        /// <summary>
        /// Create a payment record for a visit — itemized by consultation, lab, medication fees.
        /// Call this before or during checkout.
        /// Only Receptionist or HospitalAdmin can create payment records.
        /// </summary>
        [HttpPost("visit/{visitId:guid}")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Create(Guid visitId, [FromBody] CreatePaymentDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _paymentService
                .CreatePaymentAsync(visitId, dto, UserId, UserName);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        // ── Process payment ───────────────────────────────────────────
        /// <summary>
        /// Record payment for an existing payment invoice.
        /// Supports full and partial payments. Updates status automatically:
        /// Full payment → Paid | Partial → Partial (balance shown in response).
        /// Only Receptionist or HospitalAdmin can process payments.
        /// </summary>
        [HttpPost("{paymentId:guid}/process")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Process(
            Guid paymentId, [FromBody] ProcessPaymentDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _paymentService.ProcessPaymentAsync(paymentId, dto, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Get payment by visit ──────────────────────────────────────
        /// <summary>
        /// Get the payment details for a specific visit including all line items.
        /// </summary>
        [HttpGet("visit/{visitId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetByVisit(Guid visitId)
        {
            var result = await _paymentService.GetPaymentByVisitAsync(visitId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Waive payment ─────────────────────────────────────────────
        /// <summary>
        /// Waive (cancel) a payment — e.g. for emergency patients, charity cases.
        /// Fully audited. Only HospitalAdmin can waive payments.
        /// A reason must be provided.
        /// </summary>
        [HttpPost("{paymentId:guid}/waive")]
        [Authorize(Roles = "HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<PaymentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Waive(
            Guid paymentId, [FromQuery] string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(ApiResponse<string>.Failure("Waiver reason is required."));

            var result = await _paymentService.WaivePaymentAsync(paymentId, reason, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }

    /// <summary>
    /// Hospital departments — organize doctors and appointments by specialty.
    /// </summary>
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    [Produces("application/json")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        private Guid HospitalId => Guid.TryParse(
            User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;

        /// <summary>
        /// Get all active departments at this hospital with doctor count per department.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DepartmentDto>>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _departmentService.GetDepartmentsAsync(HospitalId);
            return Ok(result);
        }

        /// <summary>
        /// Create a new department at this hospital.
        /// Only HospitalAdmin can create departments.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<DepartmentDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Create(
            [FromBody] CreateDepartmentDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _departmentService
                .CreateDepartmentAsync(HospitalId, dto.Name, dto.Description);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        /// <summary>
        /// Get all doctors in a specific department.
        /// </summary>
        [HttpGet("{departmentId:guid}/doctors")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorSummaryDto>>), 200)]
        public async Task<IActionResult> GetDoctors(Guid departmentId)
        {
            var result = await _departmentService.GetDoctorsByDepartmentAsync(departmentId);
            return Ok(result);
        }

        /// <summary>
        /// Get all available doctors at this hospital today.
        /// Used by receptionist when assigning a doctor to a patient.
        /// Only returns doctors who are marked available and haven't hit their daily patient limit.
        /// </summary>
        [HttpGet("available-doctors")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<List<DoctorSummaryDto>>), 200)]
        public async Task<IActionResult> GetAvailableDoctors()
        {
            var result = await _departmentService.GetAvailableDoctorsAsync(HospitalId);
            return Ok(result);
        }
    }
}

// ── DTO needed for DepartmentController ──────────────────────────────
namespace HMS.Shared.DTOs.Visit
{
    using System.ComponentModel.DataAnnotations;
    public class CreateDepartmentDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

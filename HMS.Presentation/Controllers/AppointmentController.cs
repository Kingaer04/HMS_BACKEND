using HMS.Entities.Enums;
using HMS.Service.Contracts;
using HMS.Shared.DTOs.Appointment;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Appointment scheduling — two-step approval flow.
    /// Patient requests → Receptionist reviews → Doctor confirms.
    /// </summary>
    [ApiController]
    [Route("api/appointments")]
    [Authorize]
    [Produces("application/json")]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        private string UserId     => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private Guid   HospitalId => Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;
        private Guid   PatientId  => Guid.TryParse(User.FindFirst("PatientId")?.Value, out var id) ? id : Guid.Empty;

        // ── Patient requests appointment ──────────────────────────────
        /// <summary>
        /// Request a new appointment at a hospital.
        /// Patient can specify a preferred doctor or leave it for the receptionist to assign.
        /// Receptionists at the target hospital are notified immediately via SignalR.
        ///
        /// Approval flow:
        /// 1. Patient requests → Status: Pending
        /// 2. Receptionist approves + assigns doctor → Status: AwaitingDoctor
        /// 3. Doctor confirms → Status: Confirmed (patient notified)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Patient")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Request([FromBody] RequestAppointmentDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _appointmentService.RequestAppointmentAsync(dto, PatientId);
            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        // ── Receptionist reviews ──────────────────────────────────────
        /// <summary>
        /// Receptionist approves or rejects a pending appointment request.
        /// If approved, assigns a doctor — that doctor is notified to confirm.
        /// If rejected, patient is notified with the reason.
        /// Only Receptionist or HospitalAdmin can call this.
        /// </summary>
        [HttpPut("{appointmentId:guid}/review")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Review(
            Guid appointmentId, [FromBody] ReceptionistReviewDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _appointmentService
                .ReceptionistReviewAsync(appointmentId, dto, UserId);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Doctor responds ───────────────────────────────────────────
        /// <summary>
        /// Doctor confirms or rejects an appointment assigned to them.
        /// On confirmation, patient is notified with the confirmed date and time.
        /// Only the assigned doctor can call this.
        /// </summary>
        [HttpPut("{appointmentId:guid}/respond")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Respond(
            Guid appointmentId, [FromBody] DoctorRespondDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _appointmentService
                .DoctorRespondAsync(appointmentId, dto, UserId);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Cancel ────────────────────────────────────────────────────
        /// <summary>
        /// Cancel an appointment. Can be done by the patient, receptionist, or admin.
        /// Completed appointments cannot be cancelled.
        /// </summary>
        [HttpDelete("{appointmentId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<string>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> Cancel(
            Guid appointmentId, [FromQuery] string reason = "Cancelled by user")
        {
            var result = await _appointmentService
                .CancelAppointmentAsync(appointmentId, UserId, reason);

            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // ── Get single ────────────────────────────────────────────────
        /// <summary>Get a single appointment by ID.</summary>
        [HttpGet("{appointmentId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<AppointmentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> GetById(Guid appointmentId)
        {
            var result = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Hospital appointments ─────────────────────────────────────
        /// <summary>
        /// Get all appointments for this hospital with optional status filter.
        /// Receptionist and HospitalAdmin use this for the appointments dashboard.
        /// </summary>
        [HttpGet("hospital")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
        public async Task<IActionResult> GetHospitalAppointments(
            [FromQuery] AppointmentStatus? status)
        {
            var result = await _appointmentService
                .GetHospitalAppointmentsAsync(HospitalId, status);
            return Ok(result);
        }

        // ── Doctor's confirmed appointments ───────────────────────────
        /// <summary>
        /// Get all confirmed appointments assigned to the logged-in doctor.
        /// Used for the doctor's schedule/agenda view.
        /// </summary>
        [HttpGet("my-schedule")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
        public async Task<IActionResult> GetMySchedule()
        {
            var result = await _appointmentService.GetDoctorAppointmentsAsync(UserId);
            return Ok(result);
        }

        // ── Patient's appointments ────────────────────────────────────
        /// <summary>
        /// Get all appointments for a specific patient.
        /// Patient can see their own. Staff can see any patient's.
        /// </summary>
        [HttpGet("patient/{patientId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<List<AppointmentDto>>), 200)]
        public async Task<IActionResult> GetPatientAppointments(Guid patientId)
        {
            var result = await _appointmentService.GetPatientAppointmentsAsync(patientId);
            return Ok(result);
        }
    }
}

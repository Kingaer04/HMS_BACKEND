using HMS.Service.Contracts;
using HMS.Shared.DTOs.Visit;
using HMS.Shared.DTOs.Notification;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Hospital visit lifecycle — check-in, doctor assignment, consultation, checkout.
    /// </summary>
    [ApiController]
    [Route("api/visits")]
    [Authorize]
    [Produces("application/json")]
    public class VisitController : ControllerBase
    {
        private readonly IVisitService _visitService;

        public VisitController(IVisitService visitService)
        {
            _visitService = visitService;
        }

        private string UserId     => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string UserName   => $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
        private Guid   HospitalId => Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;

        /// <summary>
        /// Check a patient into the hospital.
        /// Creates a visit record and starts the lifecycle.
        /// Only Receptionist or HospitalAdmin can check in patients.
        /// </summary>
        [HttpPost("checkin")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 201)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var result = await _visitService.CheckInPatientAsync(dto, UserId, UserName, HospitalId);
            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        /// <summary>
        /// Assign a doctor to a checked-in patient.
        /// Sends a real-time SignalR notification to the assigned doctor immediately.
        /// </summary>
        [HttpPost("{visitId:guid}/assign-doctor")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> AssignDoctor(Guid visitId, [FromBody] AssignDoctorDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);
            var result = await _visitService.AssignDoctorAsync(visitId, dto, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Doctor grants entry to the assigned patient.
        /// Only the assigned doctor can call this.
        /// </summary>
        [HttpPost("{visitId:guid}/grant-entry")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> GrantEntry(Guid visitId)
        {
            var result = await _visitService.DoctorGrantEntryAsync(visitId, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Mark consultation as started.
        /// Only the assigned doctor can call this.
        /// </summary>
        [HttpPost("{visitId:guid}/start-consultation")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> StartConsultation(Guid visitId)
        {
            var result = await _visitService.StartConsultationAsync(visitId, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Mark consultation as ended. Patient directed to checkout.
        /// Only the assigned doctor can call this.
        /// </summary>
        [HttpPost("{visitId:guid}/end-consultation")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> EndConsultation(Guid visitId)
        {
            var result = await _visitService.EndConsultationAsync(visitId, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Check out a patient. Payment must be processed before or after this step.
        /// Only Receptionist or HospitalAdmin can checkout patients.
        /// </summary>
        [HttpPost("{visitId:guid}/checkout")]
        [Authorize(Roles = "Receptionist,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> CheckOut(Guid visitId, [FromBody] CheckOutDto dto)
        {
            var result = await _visitService.CheckOutPatientAsync(visitId, dto, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Get a single visit by ID.</summary>
        [HttpGet("{visitId:guid}")]
        [ProducesResponseType(typeof(ApiResponse<VisitDto>), 200)]
        public async Task<IActionResult> GetById(Guid visitId)
        {
            var result = await _visitService.GetVisitByIdAsync(visitId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Get all visits for today at this hospital.
        /// Receptionist dashboard uses this to show the daily queue.
        /// </summary>
        [HttpGet("today")]
        [Authorize(Roles = "Receptionist,HospitalAdmin,Doctor")]
        [ProducesResponseType(typeof(ApiResponse<List<VisitSummaryDto>>), 200)]
        public async Task<IActionResult> TodaysVisits()
        {
            var result = await _visitService.GetTodaysVisitsAsync(HospitalId);
            return Ok(result);
        }

        /// <summary>Get full visit history for a patient across all hospitals.</summary>
        [HttpGet("patient/{patientId:guid}/history")]
        [ProducesResponseType(typeof(ApiResponse<List<VisitSummaryDto>>), 200)]
        public async Task<IActionResult> PatientHistory(Guid patientId)
        {
            var result = await _visitService.GetPatientVisitHistoryAsync(patientId);
            return Ok(result);
        }
    }

    /// <summary>
    /// Real-time notifications — retrieve, mark as read.
    /// Notifications are also pushed via SignalR at /hubs/notifications.
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Produces("application/json")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        /// <summary>Get notifications for the logged-in user.</summary>
        /// <param name="unreadOnly">Set true to return only unread notifications.</param>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] bool unreadOnly = false)
        {
            var result = await _notificationService.GetUserNotificationsAsync(UserId, unreadOnly);
            return Ok(result);
        }

        /// <summary>Get count of unread notifications for the logged-in user.</summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<int>), 200)]
        public async Task<IActionResult> UnreadCount()
        {
            var result = await _notificationService.GetUnreadCountAsync(UserId);
            return Ok(result);
        }

        /// <summary>Mark a single notification as read.</summary>
        [HttpPatch("{notificationId:guid}/read")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkRead(Guid notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId, UserId);
            return Ok(new { message = "Marked as read." });
        }

        /// <summary>Mark all notifications as read for the logged-in user.</summary>
        [HttpPatch("read-all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notificationService.MarkAllAsReadAsync(UserId);
            return Ok(new { message = "All notifications marked as read." });
        }
    }
}

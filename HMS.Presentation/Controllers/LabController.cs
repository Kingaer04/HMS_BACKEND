using HMS.Service.Contracts;
using HMS.Shared.DTOs.Lab;
using HMS.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    /// <summary>
    /// Laboratory management — test requests, results, and catalogue.
    /// Doctors request tests. Lab technicians process and enter results.
    /// </summary>
    [ApiController]
    [Route("api/lab")]
    [Authorize]
    [Produces("application/json")]
    public class LabController : ControllerBase
    {
        private readonly ILabService _labService;

        public LabController(ILabService labService)
        {
            _labService = labService;
        }

        private string UserId     => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string UserName   => $"{User.FindFirst(ClaimTypes.GivenName)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
        private Guid   HospitalId => Guid.TryParse(User.FindFirst("HospitalId")?.Value, out var id) ? id : Guid.Empty;

        // ── Doctor creates lab request ────────────────────────────────
        /// <summary>
        /// Request lab tests for a patient during a visit.
        /// Only the assigned doctor for that visit can create lab requests.
        /// All lab technicians at the hospital are notified in real-time via SignalR.
        /// </summary>
        [HttpPost("requests/patient/{patientId:guid}/visit/{visitId:guid}")]
        [Authorize(Roles = "Doctor")]
        [ProducesResponseType(typeof(ApiResponse<LabRequestDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<string>), 400)]
        public async Task<IActionResult> CreateLabRequest(
            Guid patientId, Guid visitId, [FromBody] CreateLabRequestDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _labService.CreateLabRequestAsync(
                patientId, visitId, dto, UserId, UserName, HospitalId);

            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        // ── Lab tech updates request status ───────────────────────────
        /// <summary>
        /// Update the status of a lab request (e.g. SampleCollected → Processing → Completed).
        /// Only LabTechnician can update lab request status.
        /// When marked as Completed, the requesting doctor is notified in real-time.
        /// </summary>
        [HttpPut("requests/{labRequestId:guid}/status")]
        [Authorize(Roles = "LabTechnician")]
        [ProducesResponseType(typeof(ApiResponse<LabRequestDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<string>), 404)]
        public async Task<IActionResult> UpdateLabStatus(
            Guid labRequestId, [FromBody] UpdateLabResultDto dto)
        {
            var result = await _labService.UpdateLabResultAsync(
                labRequestId, dto, UserId, UserName);

            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        // ── Lab tech enters individual test result ────────────────────
        /// <summary>
        /// Enter or update the result of a specific test within a lab request.
        /// Include IsAbnormal flag to highlight abnormal values for the doctor.
        /// Only LabTechnician can enter results.
        /// </summary>
        [HttpPost("requests/{labRequestId:guid}/results")]
        [Authorize(Roles = "LabTechnician")]
        [ProducesResponseType(typeof(ApiResponse<LabTestResultDto>), 201)]
        public async Task<IActionResult> AddTestResult(
            Guid labRequestId, [FromBody] AddTestResultDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _labService.AddTestResultAsync(labRequestId, dto, UserId);
            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }

        // ── Get requests by visit ─────────────────────────────────────
        /// <summary>
        /// Get all lab requests for a specific visit.
        /// Accessible by Doctor, Receptionist, LabTechnician, and HospitalAdmin.
        /// </summary>
        [HttpGet("requests/visit/{visitId:guid}")]
        [Authorize(Roles = "Doctor,Receptionist,LabTechnician,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<List<LabRequestDto>>), 200)]
        public async Task<IActionResult> GetByVisit(Guid visitId)
        {
            var result = await _labService.GetLabRequestsByVisitAsync(visitId);
            return Ok(result);
        }

        // ── Get all pending requests at this hospital ─────────────────
        /// <summary>
        /// Get all pending and in-progress lab requests at this hospital.
        /// This is the lab technician's work queue.
        /// </summary>
        [HttpGet("requests/pending")]
        [Authorize(Roles = "LabTechnician,HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<List<LabRequestDto>>), 200)]
        public async Task<IActionResult> GetPending()
        {
            var result = await _labService.GetPendingLabRequestsAsync(HospitalId);
            return Ok(result);
        }

        // ── Lab catalogue ─────────────────────────────────────────────
        /// <summary>
        /// Get the catalogue of available lab tests at this hospital.
        /// Used by doctors when selecting tests to request.
        /// </summary>
        [HttpGet("catalogue")]
        [ProducesResponseType(typeof(ApiResponse<List<LabCatalogueItemDto>>), 200)]
        public async Task<IActionResult> GetCatalogue()
        {
            var result = await _labService.GetLabCatalogueAsync(HospitalId);
            return Ok(result);
        }

        /// <summary>
        /// Add a new test to this hospital's lab catalogue.
        /// Only HospitalAdmin can manage the catalogue.
        /// </summary>
        [HttpPost("catalogue")]
        [Authorize(Roles = "HospitalAdmin")]
        [ProducesResponseType(typeof(ApiResponse<LabCatalogueItemDto>), 201)]
        public async Task<IActionResult> AddToCatalogue([FromBody] AddLabCatalogueItemDto dto)
        {
            if (!ModelState.IsValid) return UnprocessableEntity(ModelState);

            var result = await _labService.AddLabCatalogueItemAsync(HospitalId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return StatusCode(201, result);
        }
    }
}

using HMS.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using HMS.Service.Hubs;

namespace HMS.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // This establishes the /api/Chat route
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _hubContext = hubContext;
        }

        [HttpGet("{roomId}/search")]
        public async Task<IActionResult> Search(Guid roomId, [FromQuery] string query)
        {
            return Ok(await _chatService.SearchMessagesAsync(roomId, query));
        }

        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] string newContent)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _chatService.EditMessageAsync(messageId, userId, newContent);
            if (!success) return BadRequest("Cannot edit read or non-existent message.");
            return Ok();
        }

        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _chatService.DeleteMessageAsync(messageId, userId);
            return success ? NoContent() : NotFound();
        }

        // "Internal Push" via SignalR
        [HttpPost("{roomId}/notify")]
        public async Task<IActionResult> SendInternalPush(Guid roomId, [FromBody] string targetUserId, string message)
        {
            // Broadcasts to the specific user even if they aren't in the chat room
            await _hubContext.Clients.User(targetUserId).SendAsync("ReceiveNotification", new
            {
                RoomId = roomId,
                Preview = message,
                SentAt = DateTime.UtcNow
            });
            return Ok();
        }

        /// <summary>
        /// Retrieves or creates a chat room between two users for a consultation or internal use.
        /// </summary>
        /// <param name="targetUserId">The ID of the user you want to chat with.</param>
        /// <param name="hospitalId">The current hospital context ID.</param>
        [HttpPost("rooms/{targetUserId}")]
        public async Task<IActionResult> GetOrCreateRoom(string targetUserId, [FromQuery] Guid hospitalId)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            // RoomType matches your model: Internal, Consultation, or Complaint
            var room = await _chatService.CreateOrGetChatRoomAsync(
                currentUserId,
                targetUserId,
                RoomType.Consultation,
                hospitalId);

            return Ok(room);
        }

        /// <summary>
        /// Retrieves the message history for a specific room, ordered by sent time.
        /// </summary>
        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetMessages(Guid roomId)
        {
            var messages = await _chatService.GetMessageHistoryAsync(roomId);
            return Ok(messages);
        }

        /// <summary>
        /// Marks all unread messages in a room as read for the current user.
        /// </summary>
        [HttpPost("{roomId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid roomId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            await _chatService.MarkMessagesAsReadAsync(roomId, userId);
            return NoContent();
        }
    }
}
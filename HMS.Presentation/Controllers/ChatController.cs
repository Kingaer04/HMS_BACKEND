using HMS.Entities.Models;
using HMS.Service.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HMS.Presentation.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // This establishes the /api/Chat route
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
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
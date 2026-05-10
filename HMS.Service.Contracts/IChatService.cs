using HMS.Entities.Models;

namespace HMS.Service.Contracts
{
    public interface IChatService
    {
        /// <summary>
        /// Retrieves an existing private chat room between two users or creates a new one if none exists.
        /// </summary>
        Task<ChatRoom> CreateOrGetChatRoomAsync(string userId1, string userId2, RoomType type, Guid hospitalId);

        /// <summary>
        /// Saves a new message to the database.
        /// </summary>
        Task<ChatMessage> SaveMessageAsync(Guid roomId, string senderId, string senderName, string content);

        /// <summary>
        /// Marks all messages in a room sent by others as 'Read' for the current user.
        /// </summary>
        Task MarkMessagesAsReadAsync(Guid roomId, string userId);

        /// <summary>
        /// Retrieves the full conversation history for a specific room.
        /// </summary>
        Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId);
    }
}
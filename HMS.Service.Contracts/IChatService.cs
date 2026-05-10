using HMS.Entities.Models;

public interface IChatService
{
    Task<ChatRoom> CreateOrGetChatRoomAsync(string userId1, string userId2, RoomType type, Guid hospitalId);
    Task<ChatMessage> SaveMessageAsync(Guid roomId, string senderId, string senderName, string content);
    Task MarkMessagesAsReadAsync(Guid roomId, string userId); // Ensure this name matches
    Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId); // Ensure this name matches
}
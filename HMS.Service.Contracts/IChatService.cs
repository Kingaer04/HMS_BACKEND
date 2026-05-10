using HMS.Entities.Models;

public interface IChatService
{
    Task<ChatRoom> CreateOrGetChatRoomAsync(string userId1, string userId2, RoomType type, Guid hospitalId);
    Task<ChatMessage> SaveMessageAsync(Guid roomId, string senderId, string senderName, string content, string type = "Text", string? fileData = null);
    Task MarkMessagesAsReadAsync(Guid roomId, string userId);
    Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId);

    // New Features
    Task<IEnumerable<ChatMessage>> SearchMessagesAsync(Guid roomId, string query);
    Task<bool> EditMessageAsync(Guid messageId, string userId, string newContent);
    Task<bool> DeleteMessageAsync(Guid messageId, string userId);
}
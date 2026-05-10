using HMS.Entities.Models;

public interface IChatService
{
    Task<ChatRoom> CreateOrGetChatRoomAsync(string user1Id, string user2Id, RoomType type, Guid hospitalId);
    Task<ChatMessage> SaveMessageAsync(Guid roomId, string senderId, string senderName, string content);
    Task MarkRoomAsReadAsync(Guid roomId, string userId);
    Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid roomId);
}
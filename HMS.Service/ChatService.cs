using HMS.Entities.Models;
using HMS.Repository.Data;
using Microsoft.EntityFrameworkCore;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;
    public ChatService(ApplicationDbContext context) => _context = context;

    public async Task<ChatRoom> CreateOrGetChatRoomAsync(string user1Id, string user2Id, RoomType type, Guid hospitalId)
    {
        var room = await _context.ChatRooms
            .Include(r => r.Members)
            .FirstOrDefaultAsync(r => r.Type == type && r.HospitalId == hospitalId &&
                                     r.Members.Any(m => m.UserId == user1Id) &&
                                     r.Members.Any(m => m.UserId == user2Id));

        if (room != null) return room;

        room = new ChatRoom { Id = Guid.NewGuid(), Type = type, HospitalId = hospitalId };
        room.Members.Add(new ChatRoomMember { UserId = user1Id });
        room.Members.Add(new ChatRoomMember { UserId = user2Id });

        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();
        return room;
    }

    public async Task<ChatMessage> SaveMessageAsync(Guid roomId, string senderId, string senderName, string content)
    {
        var msg = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatRoomId = roomId,
            SenderId = senderId,
            SenderName = senderName,
            Content = content,
            MessageType = "Text"
        };
        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();
        return msg;
    }

    public async Task MarkRoomAsReadAsync(Guid roomId, string userId)
    {
        var member = await _context.ChatRoomMembers
            .FirstOrDefaultAsync(m => m.ChatRoomId == roomId && m.UserId == userId);
        if (member != null)
        {
            member.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ChatMessage>> GetHistoryAsync(Guid roomId) =>
        await _context.ChatMessages.Where(m => m.ChatRoomId == roomId)
            .OrderByDescending(m => m.SentAt).Take(50).ToListAsync();
}
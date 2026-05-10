using HMS.Entities.Models;
using HMS.Repository.Data;
using HMS.Service.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HMS.Service
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;

        public ChatService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom> CreateOrGetChatRoomAsync(string userId1, string userId2, RoomType type, Guid hospitalId)
        {
            var room = await _context.ChatRooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.Type == type &&
                                         r.HospitalId == hospitalId &&
                                         r.Members.Any(m => m.UserId == userId1) &&
                                         r.Members.Any(m => m.UserId == userId2));

            if (room != null) return room;

            room = new ChatRoom
            {
                Id = Guid.NewGuid(),
                Type = type,
                HospitalId = hospitalId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Members = new List<ChatRoomMember>
                {
                    new ChatRoomMember { UserId = userId1, UserRole = "Participant" },
                    new ChatRoomMember { UserId = userId2, UserRole = "Participant" }
                }
            };

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
                MessageType = "Text",
                IsRead = false,
                SentAt = DateTime.UtcNow // Matches your ChatEntities.cs
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();
            return msg;
        }

        // Implementation for the interface member 'GetMessageHistoryAsync'
        public async Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.SentAt) // Uses SentAt from your model
                .ToListAsync();
        }

        // Implementation for the interface member 'MarkMessagesAsReadAsync'
        public async Task MarkMessagesAsReadAsync(Guid roomId, string userId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId &&
                             m.SenderId != userId &&
                             !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
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
            // Note: Updated to match your RoomType enum (Internal, Consultation, Complaint)
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
                Members = new List<ChatRoomMember>
                {
                    new ChatRoomMember { UserId = userId1, UserRole = "Participant" }, // Added UserRole per your model
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
                SentAt = DateTime.UtcNow, // Matches your ChatEntities.cs
                IsRead = false,
                MessageType = "Text"
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();
            return msg;
        }

        public async Task<IEnumerable<ChatMessage>> GetMessageHistoryAsync(Guid roomId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId)
                .OrderBy(m => m.SentAt) // Updated from Timestamp to SentAt
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(Guid roomId, string userId)
        {
            var unread = await _context.ChatMessages
                .Where(m => m.ChatRoomId == roomId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var m in unread) m.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
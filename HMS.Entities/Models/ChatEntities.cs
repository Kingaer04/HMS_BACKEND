namespace HMS.Entities.Models
{
    public enum RoomType { Internal, Consultation, Complaint }
    public enum UserStatus { Offline, Online, Away, Busy }

    public class ChatRoom
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public RoomType Type { get; set; }
        public Guid HospitalId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public ICollection<ChatRoomMember> Members { get; set; } = new List<ChatRoomMember>();
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }

    public class ChatRoomMember
    {
        public Guid ChatRoomId { get; set; }
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }
    }

    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid ChatRoomId { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string MessageType { get; set; } // "Text", "Image", "File"
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
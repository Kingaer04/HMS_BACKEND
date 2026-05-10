using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System.Security.Claims;
using HMS.Entities.Models;

namespace HMS.Service.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private static readonly ConcurrentDictionary<string, string> _onlineUsers = new();

        public ChatHub(IChatService chatService) => _chatService = chatService;

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                _onlineUsers.TryAdd(userId, Context.ConnectionId);
                await Clients.Others.SendAsync("UserStatusChanged", userId, "Online");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                _onlineUsers.TryRemove(userId, out _);
                await Clients.Others.SendAsync("UserStatusChanged", userId, "Offline");
            }
            await base.OnDisconnectedAsync(ex);
        }

        public async Task SendMessage(string roomId, string content)
        {
            var userId = Context.UserIdentifier!;
            var userName = Context.User?.Identity?.Name ?? "Staff";
            var msg = await _chatService.SaveMessageAsync(Guid.Parse(roomId), userId, userName, content);
            await Clients.Group(roomId).SendAsync("ReceiveMessage", msg);
        }

        public async Task JoinRoom(string roomId) => await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        public async Task Typing(string roomId) =>
            await Clients.OthersInGroup(roomId).SendAsync("UserIsTyping", roomId, Context.UserIdentifier);

        public async Task StoppedTyping(string roomId) =>
            await Clients.OthersInGroup(roomId).SendAsync("UserStoppedTyping", roomId, Context.UserIdentifier);

        public async Task UpdateStatus(string status) =>
            await Clients.Others.SendAsync("UserStatusChanged", Context.UserIdentifier, status);
    }
}
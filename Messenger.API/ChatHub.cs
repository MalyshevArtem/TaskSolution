using Messenger.API.Models;
using Messenger.API.Repositories;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Messenger.API
{
    public class ChatHub : Hub
    {
        private readonly MessageRepository _messageRepo;
        private readonly GroupRepository _groupRepo;
        private readonly GroupMessageRepository _groupMessageRepo;
        private readonly IConnectionMultiplexer _redis;

        public ChatHub(
            MessageRepository messageRepo,
            GroupRepository groupRepo,
            GroupMessageRepository groupMessageRepo,
            IConnectionMultiplexer redis)
        {
            _messageRepo = messageRepo;
            _groupRepo = groupRepo;
            _groupMessageRepo = groupMessageRepo;
            _redis = redis;
        }

        public async Task SendPrivateMessage(string toUserId, string content)
        {
            var fromUserId = Context.UserIdentifier;

            var message = new Message()
            {
                Id = Guid.NewGuid(),
                FromUserId = fromUserId,
                ToUserId = toUserId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsGroupMessage = false,
                IsEdited = false,
                IsDeleted = false,
                IsRead = false,
                IsNotified = false,
            };

            await _messageRepo.SaveMessageAsync(message);
            await Clients.Users(toUserId).SendAsync("ReceiveMessage", message.Id, fromUserId, content);
        }

        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

            var userId = Context.UserIdentifier!;
            await _groupRepo.AddUserToGroupAsync(groupId, userId);
        }

        public async Task SendGroupMessage(string groupId, string content)
        {
            var fromUserId = Context.UserIdentifier!;

            var message = new Message()
            {
                Id = Guid.NewGuid(),
                FromUserId = fromUserId,
                ToUserId = groupId,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsGroupMessage = true,
                IsEdited = false,
                IsDeleted = false,
                IsRead = false,
                IsNotified = false,
            };

            await _messageRepo.SaveMessageAsync(message);

            var groupMembers = await _groupRepo.GetGroupMembersAsync(groupId, fromUserId);

            foreach (var gm in groupMembers)
            {
                await _groupMessageRepo.AddGroupMessageIdToUser(message.Id, gm.UserId);
            }

            await Clients.GroupExcept(groupId, Context.ConnectionId)
                .SendAsync("ReceiveGroupMessage", message.Id, fromUserId, content);
        }

        public async Task MarkMessageAsRead(Guid messageId)
        {
            await _messageRepo.MarkMessageAsReadAsync(messageId);
        }

        public async Task MarkGroupMessageAsRead(Guid messageId)
        {
            var userId = Context.UserIdentifier!;
            await _groupMessageRepo.MarkMessageAsReadAsync(messageId, userId);
        }

        public async Task LoadHistory(string otherUserId)
        {
            var myUserId = Context.UserIdentifier!;
            var messages = await _messageRepo.GetHistoryAsync(myUserId, otherUserId);

            foreach (var m in messages)
            {
                if (m.ToUserId == myUserId && !m.IsRead)
                {
                    await _messageRepo.MarkMessageAsReadAsync(m.Id);
                    m.IsRead = true;
                }
            }

            await Clients.Caller.SendAsync("ReceiveHistory", messages);
        }

        public async Task LoadGroupHistory(string groupId)
        {
            var userId = Context.UserIdentifier!;
            var messages = await _messageRepo.GetGroupHistoryAsync(groupId);

            foreach (var m in messages)
            {
                var gm = await _groupMessageRepo.GetGroupMessage(m.Id, userId);

                if (gm != null && !gm.IsRead)
                {
                    await _groupMessageRepo.MarkMessageAsReadAsync(gm.MessageId, userId);
                    m.IsRead = true;
                }
            }

            await Clients.Caller.SendAsync("ReceiveHistory", messages);
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Connected: {Context.ConnectionId}");

            var userId = Context.UserIdentifier;

            if (userId != null)
            {
                var db = _redis.GetDatabase();
                await db.SetAddAsync("online_users", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Disconnected: {Context.ConnectionId}");

            var userId = Context.UserIdentifier;

            if (userId != null)
            {
                var db = _redis.GetDatabase();
                await db.SetRemoveAsync("online_users", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

using Messenger.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Messenger.Client
{
    public class Program
    {
        private static HubConnection? _connection;
        private static string? _userId;
        private static string? _toUserId;
        private static string? _groupId;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter your user Id: ");
            _userId = Console.ReadLine();

             _connection = new HubConnectionBuilder()
            .WithUrl($"http://localhost:5000/chatHub?userId={_userId}")
            .WithAutomaticReconnect()
            .Build();


            _connection.On<Guid, string, string>("ReceiveMessage", async (messageId, fromUserId, content) =>
            {
                Console.WriteLine($"{fromUserId}: {content} ({DateTime.UtcNow:t})");
                await _connection.InvokeAsync("MarkMessageAsRead", messageId);
            });


            _connection.On<Guid, string, string>("ReceiveGroupMessage", async (messageId, fromUserId, content) =>
            {
                Console.WriteLine($"{fromUserId}: {content} ({DateTime.UtcNow:t})");
                await _connection.InvokeAsync("MarkGroupMessageAsRead", messageId);
            });


            _connection.On<IEnumerable<Message>>("ReceiveHistory", messages =>
            {
                Console.WriteLine("\n=== Chat History ===");

                foreach (var message in messages)
                {
                    Console.WriteLine($"{message.FromUserId}: {message.Content} ({message.Timestamp:t})");
                }

                Console.WriteLine("====================");
            });


            try
            {
                await _connection!.StartAsync();
                Console.WriteLine("Connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting: {ex.Message}");
                return;
            }


            while (true)
            {
                Console.WriteLine("Select mode: Enter 1 for private chat - or - Enter 2 for group chat");
                var mode = Console.ReadLine();

                if (mode == "1")
                {
                    Console.WriteLine("Enter user name to chat with: ");
                    _toUserId = Console.ReadLine();

                    await _connection.InvokeAsync("LoadHistory", _toUserId);
                }
                else if (mode == "2")
                {
                    Console.WriteLine("Enter group name to chat in: ");
                    _groupId = Console.ReadLine();

                    await _connection.InvokeAsync("LoadGroupHistory", _groupId);
                    await _connection.InvokeAsync("JoinGroup", _groupId);
                }

                Console.WriteLine("Enter '/mode' to exit the current chat");

                while (true)
                {
                    var content = Console.ReadLine();

                    if (content == "/mode")
                    {
                        break;
                    }

                    try
                    {
                        if (mode == "1")
                        {
                            await _connection.InvokeAsync("SendPrivateMessage", _toUserId, content);
                        }
                        else if (mode == "2")
                        {
                            await _connection.InvokeAsync("SendGroupMessage", _groupId, content);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message: {ex.Message}");
                    }
                }
            }
        }
    }
}

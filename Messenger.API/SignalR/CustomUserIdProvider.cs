using Microsoft.AspNetCore.SignalR;

namespace Messenger.API.SignalR
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var httpContext = connection.GetHttpContext();
            return httpContext?.Request.Query["userId"];
        }
    }
}

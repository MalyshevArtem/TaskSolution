using Messenger.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Messenger.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly MessageRepository _messageRepo;

        public MessagesController(MessageRepository messageRepo)
        {
            _messageRepo = messageRepo;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] string newContent)
        {
            var success = await _messageRepo.EditMessageAsync(id, newContent);
            return success ? Ok() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            var success = await _messageRepo.DeleteMessageAsync(id);
            return success ? Ok() : NotFound();
        }

        [HttpGet("online")]
        public async Task<IEnumerable<string>> GetOnlineUsers([FromServices] IConnectionMultiplexer redis)
        {
            var db = redis.GetDatabase();
            var users = await db.SetMembersAsync("online_users");
            return users.Select(u => u.ToString());
        }
    }
}

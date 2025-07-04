using System.Data.SqlClient;
using Dapper;
using Messenger.API.Models;

namespace Messenger.API.Repositories
{
    public class GroupMessageRepository
    {
        private readonly string _connectionString;

        public GroupMessageRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task AddGroupMessageIdToUser(Guid messageId, string userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                insert into GroupMessageReads (MessageId, UserId)
                values (@MessageId, @UserId)";

                await conn.ExecuteAsync(sql, new { MessageId = messageId, UserId = userId });
            }
        }

        public async Task MarkMessageAsReadAsync(Guid messageId, string userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                update GroupMessageReads
                set IsRead = 1
                where MessageId = @MessageId and UserId = @UserId";

                await conn.ExecuteAsync(sql, new { MessageId = messageId, UserId = userId });
            }
        }

        public async Task<GroupMessage?> GetGroupMessage(Guid messageId, string userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                select * from GroupMessageReads
                where MessageId = @MessageId and UserId = @UserId";

                return await conn.QuerySingleOrDefaultAsync<GroupMessage>(sql, new { MessageId = messageId, UserId = userId });
            }
        }
    }
}

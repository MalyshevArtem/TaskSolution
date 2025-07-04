using System.Data.SqlClient;
using Dapper;
using Messenger.API.Models;


namespace Messenger.API.Repositories
{
    public class MessageRepository
    {
        private readonly string _connectionString;

        public MessageRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task SaveMessageAsync(Message message)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                insert into Messages
                (Id, FromUserId, ToUserId, Content, Timestamp, IsGroupMessage, IsEdited, IsDeleted, IsRead, IsNotified)
                values
                (@Id, @FromUserId, @ToUserId, @Content, @Timestamp, @IsGroupMessage, @IsEdited, @IsDeleted, @IsRead, @IsNotified)";

                await conn.ExecuteAsync(sql, message);
            }
        }

        public async Task<IEnumerable<Message>> GetHistoryAsync(string userId1, string userId2)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                select * from Messages
                where
                ((FromUserId = @UserId1 and ToUserId = @UserId2)
                or
                (FromUserId = @UserId2 and ToUserId = @UserId1))
                and
                IsDeleted = 0
                order by Timestamp asc";

                return await conn.QueryAsync<Message>(sql, new { UserId1 = userId1, UserId2 = userId2 });
            }
        }

        public async Task<IEnumerable<Message>> GetGroupHistoryAsync(string groupId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                select * from Messages
                where
                ToUserId = @GroupId and
                IsGroupMessage = 1 and
                IsDeleted = 0
                order by Timestamp asc";

                return await conn.QueryAsync<Message>(sql, new { GroupId = groupId });
            }
        }

        public async Task<bool> EditMessageAsync(Guid id, string newContent)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                update Messages
                set Content = @Content, IsEdited = 1
                where Id = @Id and IsDeleted = 0";

                var rows = await conn.ExecuteAsync(sql, new { Content = newContent, Id = id });
                return rows > 0;
            }
        }

        public async Task<bool> DeleteMessageAsync(Guid id)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                update Messages
                set IsDeleted = 1
                where Id = @Id";

                var rows = await conn.ExecuteAsync(sql, new { Id = id });
                return rows > 0;
            }
        }

        public async Task MarkMessageAsReadAsync(Guid messageId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                update Messages
                set IsRead = 1
                where Id = @Id";

                await conn.ExecuteAsync(sql, new { Id = messageId });
            }
        }
    }
}

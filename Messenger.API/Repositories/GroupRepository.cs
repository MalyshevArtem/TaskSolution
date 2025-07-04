using System.Data.SqlClient;
using Dapper;
using Messenger.API.Models;

namespace Messenger.API.Repositories
{
    public class GroupRepository
    {
        private readonly string _connectionString;

        public GroupRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IEnumerable<GroupMember>> GetGroupMembersAsync(string groupId, string userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                select * from GroupMembers
                where GroupId = @GroupId and UserId != @UserId";

                return await conn.QueryAsync<GroupMember>(sql, new { GroupId = groupId, UserId = userId });
            }
        }

        public async Task AddUserToGroupAsync(string groupId, string userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var sql = @"
                select count(*) from GroupMembers
                where GroupId = @GroupId and UserId = @UserId";

                var count = await conn.ExecuteScalarAsync<int>(sql, new { GroupId = groupId, UserId = userId });

                if (count == 0)
                {
                    sql = @"
                    insert into GroupMembers (GroupId, UserId)
                    values (@GroupId, @UserId)";

                    await conn.ExecuteAsync(sql, new { GroupId = groupId, UserId = userId });
                }
            }
        }
    }
}

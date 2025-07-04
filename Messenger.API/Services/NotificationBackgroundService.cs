
using System.Data.SqlClient;
using Dapper;
using Messenger.API.Models;
using Microsoft.VisualBasic;
using StackExchange.Redis;

namespace Messenger.API.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IConfiguration _configuration;
        private readonly MockEmailSender _emailSender;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            IConnectionMultiplexer redis,
            IConfiguration configuration,
            MockEmailSender emailSender,
            ILogger<NotificationBackgroundService> logger)
        {
            _redis = redis;
            _configuration = configuration;
            _emailSender = emailSender;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var db = _redis.GetDatabase();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var conn = new SqlConnection(connectionString))
                    {
                        // приватные сообщения
                        var sql = @"
                        select * from Messages
                        where IsNotified = 0 and
                        IsRead = 0 and
                        IsGroupMessage = 0 and
                        Timestamp <= dateadd(minute, -5, getutcdate())";

                        var messages = await conn.QueryAsync<Message>(sql);

                        foreach (var m in messages)
                        {
                            var online = await db.SetContainsAsync("online_users", m.ToUserId);

                            if (!online)
                            {
                                await _emailSender.SendEmailAsync(m.ToUserId);
                                await conn.ExecuteAsync("update Messages set IsNotified = 1 where Id = @Id", new { m.Id });
                            }
                        }

                        // групповые сообщения
                        sql = @"
                        select a.* from GroupMessageReads a
                        join Messages b
                        on a.MessageId = b.Id
                        where a.IsNotified = 0 and
                        a.IsRead = 0 and
                        b.Timestamp <= dateadd(minute, -5, getutcdate())";

                        var groupMessages = await conn.QueryAsync<GroupMessage>(sql);

                        foreach (var gm in groupMessages)
                        {
                            var online = await db.SetContainsAsync("online_users", gm.UserId);

                            if (!online)
                            {
                                await _emailSender.SendEmailAsync(gm.UserId);
                                await conn.ExecuteAsync(@"
                                update GroupMessageReads
                                set IsNotified = 1
                                where MessageId = @MessageId and UserId = @UserId",
                                new { gm.MessageId, gm.UserId });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in NotificationBackgroundService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}

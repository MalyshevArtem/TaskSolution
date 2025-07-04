
using Messenger.API.Repositories;
using Messenger.API.Services;
using Messenger.API.SignalR;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace Messenger.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();  
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
            builder.Services.AddSingleton<MessageRepository>();
            builder.Services.AddSingleton<GroupRepository>();
            builder.Services.AddSingleton<GroupMessageRepository>();

            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse("localhost:6379", true);
                return ConnectionMultiplexer.Connect(config);
            });

            builder.Services.AddHostedService<NotificationBackgroundService>();
            builder.Services.AddSingleton<MockEmailSender>();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseCors();

            app.MapHub<ChatHub>("/chatHub");


            app.MapControllers();

            app.Run();
        }
    }
}

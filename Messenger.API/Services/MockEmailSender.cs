namespace Messenger.API.Services
{
    public class MockEmailSender
    {
        private readonly ILogger<MockEmailSender> _logger;

        public MockEmailSender(ILogger<MockEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string userId)
        {
            var subject = "you've got an unread message!";

            _logger.LogInformation($"[EMAIL]: {userId}, {subject}");

            return Task.CompletedTask;
        }
    }
}

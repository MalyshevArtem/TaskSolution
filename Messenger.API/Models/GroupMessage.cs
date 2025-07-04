namespace Messenger.API.Models
{
    public class GroupMessage
    {
        public Guid MessageId { get; set; }
        public string? UserId { get; set; }
        public bool IsRead { get; set; }
        public bool IsNotified { get; set; }
    }
}

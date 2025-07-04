namespace Messenger.API.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public string? FromUserId { get; set; }
        public string? ToUserId { get; set; }
        public string? Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsGroupMessage { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
        public bool IsNotified { get; set; }
    }
}

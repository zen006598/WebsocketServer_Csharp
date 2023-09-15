namespace serverapiorg.Models
{
    public class WSMsgInfo
    {
        public MessageType _Type { get; set; }
        public string Sender { get; set; }
        public string Reciever { get; set; }
        public string Content { get; set; }
    }
    public enum MessageType
    {
        Broadcast, _Private
    }
}
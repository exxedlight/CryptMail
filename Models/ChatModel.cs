namespace CryptMail.Models
{
    public class ChatModel
    {
        public List<MessageModel> messages { get; set; } = new List<MessageModel>();

        public NewMessageModel newMessage { get; set; } = new NewMessageModel();
        public int currentPage { get; set; } = 1;
    }
}
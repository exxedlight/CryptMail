using CryptMail.MSSQLServer;

namespace CryptMail.Models
{
    public class UserMessageModel
    {
        public UserModel userModel { get; set; } = new UserModel("", "");
        public NewMessageModel messageModel { get; set; } = new NewMessageModel();

        public bool newMessageFormOpened { get; set; } = false;
        public bool isAnyChat { get; set; } = false;

        public List<MessageModel> messagesList { get; set; } = new List<MessageModel>();
    }
}

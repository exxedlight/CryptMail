using CryptMail.MSSQLServer;
using Microsoft.AspNetCore.Mvc;

namespace CryptMail.Models
{
    public class MessageModel
    {

        //  message ID
        public Guid? Id { get; set; } = null;


        //  sender / reciever ID`s
        public Guid? userFromId { get; set; } = null;
        public Guid? userToId { get; set; } = null;

        //  message data
        public string recieverLogin { get; set; } = "";
        public string senderLogin { get; set; } = "";
        public string PartnerLogin { get; set; } = "";

        public string Theme { get; set; } = "";
        public string Message { get; set; } = "";

        public DateTime messageSendTime { get; set; }

        //  flags (readed and favorite marks)
        public bool isReadedByReciever { get; set; } = false;
        public bool isImportantForSender { get; set; } = false;
        public bool isImportantForReciever { get; set; } = false;
    }

    public class NewMessageModel : MessageModel
    {
        [BindProperty]
        public string? key { get; set; } = null;
        
        [BindProperty]
        public int? alg { get; set; } = null;
        public string? onFormMessage { get; set; } = null;
        public string onFormMessageColor = "red";
    }
}

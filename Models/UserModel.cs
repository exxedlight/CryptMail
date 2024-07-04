namespace CryptMail.Models
{
    public class UserModel
    {
        public string Id { get; set; }
        public string login { get; set; }

        public UserModel(string id, string login)
        {
            Id = id;
            this.login = login;
        }
    }
}

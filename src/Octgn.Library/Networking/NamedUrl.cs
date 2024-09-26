using Octgn.Communication;

namespace Octgn.Library.Networking
{
    public class NamedUrl
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool HasCredentials => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Password);

        public NamedUrl(string name, string url, string username, string password)
        {
            Url = url;
            Name = name;
            Username = username;
            Password = password;
        }
    }
}
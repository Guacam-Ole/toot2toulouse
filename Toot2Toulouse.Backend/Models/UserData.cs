using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class UserData
    {
        public UserConfiguration Config { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public Twitter Twitter { get; set; }
        public Mastodon Mastodon { get; set; }
        public List<Crosspost> Crossposts { get; set; }  =new List<Crosspost>();

        public override string ToString()
        {
            return $"[{Id}|{Mastodon?.Id}|{Twitter?.Id}]";
        }
    }
}

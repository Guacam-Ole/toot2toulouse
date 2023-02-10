using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class UserData
    {
        public enum BlockReasons
        {
            Manual,
            AuthTwitter,
            AuthMastodon
        }

        public UserConfiguration Config { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public Twitter Twitter { get; set; } = new Twitter();
        public Mastodon Mastodon { get; set; } = new Mastodon();
        public List<Crosspost> Crossposts { get; set; } = new List<Crosspost>();
        public DateTime? BlockDate { get; set; }
        public BlockReasons? BlockReason { get; set; }
        


        public override string ToString()
        {
            return $"[{Id}|{Mastodon?.Id}|{Twitter?.Id}]";
        }
    }
}

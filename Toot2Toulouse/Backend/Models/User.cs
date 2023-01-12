using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class User
    {
        public UserConfiguration Config { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Hash { get; set; }
        public Twitter Twitter { get; set; }
        public Mastodon Mastodon { get; set; }
    }
}

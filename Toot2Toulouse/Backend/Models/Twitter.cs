using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class Twitter
    {
        [HideOnExport]
        public string Secret { get; set; }
        public string Id { get; set; }
        public string Handle { get; set; }
        public DateTime? LastTweetDate { get; set; }
        public long LastTweetId { get; set; }
    }
}

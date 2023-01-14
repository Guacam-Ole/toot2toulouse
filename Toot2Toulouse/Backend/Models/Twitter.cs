using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class Twitter
    {
        [HideOnExport]
        public string ConsumerSecret { get; set; }
        [HideOnExport]
        public string ConsumerKey { get; set; }
        [HideOnExport]
        public string AccessToken { get; set; }
        [HideOnExport]
        public string AccessSecret { get; set; }
        [HideOnExport]
        public string Bearer { get; set; }
        public string Id { get; set; }
        public string Handle { get; set; }
        public string DisplayName { get; set; }
        public DateTime? LastTweetDate { get; set; }
        public long LastTweetId { get; set; }
    }
}

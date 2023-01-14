using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Models
{
    public class Mastodon
    {
        public string Instance { get; set; }
        [HideOnExport]
        public string Secret { get; set; }  
        public string Id { get; set; }
        public string Handle { get; set; }
        public string DisplayName { get; set; }
        public long LastToot { get; set; }
        public DateTime? LastTootDate { get; set; }
    }
}

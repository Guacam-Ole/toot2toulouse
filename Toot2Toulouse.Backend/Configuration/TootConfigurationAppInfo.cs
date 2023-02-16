namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationAppInfo
    {
        [OverviewCategory("General Information")]
        public string Instance { get; set; }

        [OverviewCategory("General Information", "App Name")]
        public string ClientName { get; set; }

        [OverviewCategory("General Information", "Mastodon Account name")]
        public string AccountName { get; set; }

        [OverviewCategory("General Information", "URL")]
        public string Url { get; set; }

        [OverviewCategory("General Information", "Suffix")]
        public string Suffix { get; set; }  // Suffix that is used when sending System messages
    }
}
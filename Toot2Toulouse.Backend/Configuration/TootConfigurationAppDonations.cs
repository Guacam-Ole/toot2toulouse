namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationAppDonations
    {
        public bool Enabled { get; set; }
        public string Caption { get; set; }
        public List<TootConfigurationAppDonationTarget> Targets { get; set; }
    }

    public class TootConfigurationAppDonationTarget
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
    }
}
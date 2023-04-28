namespace Toot2Toulouse.Backend.Models
{
    public class PingData
    {
        public string Url { get; set; }
        public Stats Stats { get; set; }
        public Configuration.TootConfigurationApp Config { get; set; }
    }
}
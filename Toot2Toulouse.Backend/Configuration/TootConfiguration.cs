namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfiguration
    {
        public TootConfigurationSecrets Secrets { get; set; }
        public TootConfigurationApp App { get; set; }
        public UserConfiguration Defaults { get; set; }
        public Version CurrentVersion { get; set; }


    }
}

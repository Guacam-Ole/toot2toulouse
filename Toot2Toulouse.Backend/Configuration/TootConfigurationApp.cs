using System.Text.Json.Serialization;

namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationApp
    {
  

        public string Disclaimer { get; set; }

        public TootConfigurationAppDonations Donations { get; set; }

        public TootConfigurationAppStats Stats { get; set; }

        public TootConfigurationAppLang Languages { get; set; }

        public TootConfigurationAppModes Modes { get; set; }

        public TootConfigurationAppInfo AppInfo { get; set; }

        public TootConfigurationAppIntervals Intervals { get; set; }

        public TootConfigurationAppTwitterLimits TwitterLimits { get; set; }

      
    }
}
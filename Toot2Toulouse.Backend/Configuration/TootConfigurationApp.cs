using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationApp
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum MessageCodes
        {
            MastodonDown,
            TwitterDown,
            MastodonAuthError,
            TwitterAuthError,
            UpAndRunning,
            BackAgain,
            RegistrationFinished,
            Invite,
            RateLimit
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum InterValNames
        {
            Never,
            Daily,
            Weekly,
            Monthly
        }

        public string Disclaimer { get; set; }  

        public string[] Ping { get; set; }
        public InterValNames GenerateStats { get; set; }

        [OverviewCategory("General Information")]
        public string Instance { get; set; }

        [OverviewCategory("General Information", "App Name")]
        public string ClientName { get; set; }

        [OverviewCategory("General Information", "Mastodon Account name")]
        public string AccountName { get; set; }

        public string Url { get; set; }

        [OverviewCategory("Twitter Attachments", "Maximum Filesize for static images", Suffix = " MB")]
        public int MaxImageSize { get; set; }

        [OverviewCategory("Twitter Attachments", "Maximum Filesize for GIFs", Suffix = " MB")]
        public int MaxGifSize { get; set; }

        [OverviewCategory("Twitter Attachments", "Maximum Filesize for embedded Videos", Suffix =" MB")]
        public int MaxVideoSize { get; set; }

        public int TwitterCharacterLimit { get; set; }
        public int MinSplitLength { get; set; } // when trying to split long toots by space this is number of characters the algorithm gives up and splits inside a word
        public string ServiceAppSuffix { get; set; }  // Suffix that is used when sending System messages

        public TootConfigurationAppIntervals Intervals { get; set; }

        public string DefaultLanguage { get; set; }
        public string[] AvailableLanguages { get; set; }

        public TootConfigurationAppModes Modes { get; set; }
    }

    public class TootConfigurationAppModes
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ValidModes
        {
            Open,
            Closed,
            [Display(Name="Invite required")]
            Invite
        }

        [OverviewCategory("Registration Limits", "New Registrations")]
        public ValidModes Active { get; set; }

        public int AutoInvite { get; set; } // Active Users per Hour before automatically switching  to Invite (<=0=disable)
        public int AutoClosed { get; set; }// Active Users per Hour before automatically switching to Closed (<=0=disable)

        [OverviewCategory("Registration Limits", "Allowed Instances", NullText ="Any")]
        public string AllowedInstances { get; set; }

        [OverviewCategory("Registration Limits", "Blocked Instances", NullText = "None")]
        public string BlockedInstances { get; set; }

        [OverviewCategory("Registration Limits", "Allow Bots")]
        public bool AllowBots { get; set; }

        [OverviewCategory("Registration Limits", "Max toots per day")]
        public long MaxTootsPerDay { get; set; }
    }

    public class TootConfigurationAppIntervals
    {
        [OverviewCategory("Intervals", "Look for new Toots", Suffix  = " (hh:mm:ss)")]
        public TimeSpan Sending { get; set; }   // How often should t2t check for message and send?

        public TimeSpan FollowerCheckApp { get; set; }  // How often should t2t scan for follower informations?
        public TimeSpan FollwerCheckUser { get; set; }  // How often should t2t scan for follower informations (per user)?

        [OverviewCategory("Intervals", "Minimum Delay", Suffix = " (hh:mm:ss)")]
        public TimeSpan MinDelay { get; set; } // Minimum delay before tweeting a toot

        [OverviewCategory("Intervals", "Maximum Delay", Suffix = " (hh:mm:ss)")]
        public TimeSpan MaxDelay { get; set; } // Maximum delay before tweeting a toot
        public int AuthFailureDeleteDays { get; set; } = 14;
    }
}
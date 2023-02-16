using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationAppModes
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ValidModes
        {
            Open,
            Closed,

            [Display(Name = "Invite required")]
            Invite
        }

        [OverviewCategory("Registration Limits", "New Registrations")]
        public ValidModes Active { get; set; }

        public int AutoInvite { get; set; } // Active Users per Hour before automatically switching  to Invite (<=0=disable)

        public int AutoClosed { get; set; }// Active Users per Hour before automatically switching to Closed (<=0=disable)

        [OverviewCategory("Registration Limits", "Allowed Instances", NullText = "Any")]
        public string AllowedInstances { get; set; }

        [OverviewCategory("Registration Limits", "Blocked Instances", NullText = "None")]
        public string BlockedInstances { get; set; }

        [OverviewCategory("Registration Limits", "Allow Bots")]
        public bool AllowBots { get; set; }

        [OverviewCategory("Registration Limits", "Max toots per day")]
        public long MaxTootsPerDay { get; set; }
    }
}
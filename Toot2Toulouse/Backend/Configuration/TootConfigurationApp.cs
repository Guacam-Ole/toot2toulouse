﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationApp
    {
        public enum MessageCodes
        {
            MastodonDown,
            TwitterDown,
            MastodonAuthError,
            TwitterAuthError,
            UpAndRunning,
            BackAgain
        }

        [OverviewCategory("General Information")]
        public string Instance { get; set; }

        [OverviewCategory("General Information", "App Name")]
        public string ClientName { get; set; }

        public string Url { get; set; }

        [OverviewCategory("Twitter Attachments (Size in MB)", "Maximum Filesize for static images")]
        public int MaxImageSize { get; set; }

        [OverviewCategory("Twitter Attachments (Size in MB)", "Maximum Filesize for GIFs")]
        public int MaxGifSize { get; set; }

        [OverviewCategory("Twitter Attachments (Size in MB)", "Maximum Filesize for embedded Videos")]
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
        [OverviewCategory("Intervals (hours:minutes:seconds)", "Look for new Toots")]
        public TimeSpan Sending { get; set; }   // How often should t2t check for message and send?

        public TimeSpan FollowerCheckApp { get; set; }  // How often should t2t scan for follower informations?
        public TimeSpan FollwerCheckUser { get; set; }  // How often should t2t scan for follower informations (per user)?

        [OverviewCategory("Intervals (hours:minutes:seconds)", "Minimum Delay")]
        public TimeSpan MinDelay { get; set; } // Minimum delay before tweeting a toot

        [OverviewCategory("Intervals (hours:minutes:seconds)", "Maximum Delay")]
        public TimeSpan MaxDelay { get; set; } // Maximum delay before tweeting a toot
    }
}
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

        public string Instance { get; set; }
        public string ClientName { get; set; }
        public string Url { get; set; }
        public Dictionary<MessageCodes, string> Messages { get;set; }
        public int TwitterCharacterLimit { get; set; }
        public int MinSplitLength { get; set; } // when trying to split long toots by space this is number of characters the algorithm gives up and splits inside a word
        public string ServiceAppSuffix { get; set; }  // Suffix that is used when sending System messages
        

        public TootConfigurationAppIntervals Intervals { get; set; }
    }


    public class TootConfigurationAppModes
    {
        public enum ValidModes
        {
            Open,
            Closed,
            Invite
        }

        public ValidModes Active { get; set; }
        public int AutoInvite { get; set; } // Active Users per Hour before automatically switching  to Invite (<=0=disable)
        public int AutoClosed { get; set; }// Active Users per Hour before automatically switching to Closed (<=0=disable)
    }


    public class TootConfigurationAppIntervals
    {
        public TimeSpan Sending { get; set; }   // How often should t2t check for message and send?
        public TimeSpan FollowerCheckApp { get; set; }  // How often should t2t scan for follower informations?
        public TimeSpan FollwerCheckUser { get; set; }  // How often should t2t scan for follower informations (per user)?
        public TimeSpan MinDelay { get; set; } // Minimum delay before tweeting a toot
        public TimeSpan MaxDelay { get; set; } // Maximum delay before tweeting a toot
    }
    
}

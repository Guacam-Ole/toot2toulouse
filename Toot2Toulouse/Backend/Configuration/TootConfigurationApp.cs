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
        public int MinSplitLength { get; set; }
        public string ServiceAppSuffix { get; set; }
        

        public TootConfigurationAppIntervals Intervals { get; set; }
    }



    public class TootConfigurationAppIntervals
    {
        public TimeSpan Sending { get; set; }
        public TimeSpan FollowerCheckApp { get; set; }
        public TimeSpan FollwerCheckUser { get; set; }
        public TimeSpan MinDelay { get; set; }
        public TimeSpan MaxDelay { get; set; }
    }
    
}

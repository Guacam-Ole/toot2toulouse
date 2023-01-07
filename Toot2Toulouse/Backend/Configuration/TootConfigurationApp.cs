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
        public string Suffix { get; set; }  
        public Dictionary<MessageCodes, string> Messages { get;set; }

        public TimeSpan SendInterval { get; set; }
        public TimeSpan FollowerCheckIntervalApp { get; set; }
        public TimeSpan FollwerCheckIntervalUser { get; set; }
        public TimeSpan MinDelay { get; set; }
        public TimeSpan MaxDelay { get; set; }
        
    }
}

namespace Toot2Toulouse.Backend.Data
{
    public class User
    {
        public enum SearchOptions
        {
            TwitterContacts,
            TwitterContactsDescription,
            Custom,
            Others,
            TextOnly,
            Backlink
        }

        public enum MastodonVisibility
        {
            Public,
            NotListed,
            Followers,
            Mentioned,
            Private
        }

        public enum TwitterVisibility
        {
            Public,
            Circle,
            Followed,
            Mentioned
        }

        public enum ConnectionStates
        {
            None,
            Success,
            ServerError,
            NotAuthenticated
        }

        
        public string MastodonHandle{ get; set; } // Unique Key

        public List<SearchOptions> ContactOptions { get; set; }
        public Dictionary<MastodonVisibility, TwitterVisibility> Visibilities { get; set; }
        public string AuthCode { get; set; }
        public DateTime? LastSuccessFullPost { get; set; }
        public ConnectionStates LastTwitterState { get; set; }
        public ConnectionStates LastMastodonState { get; set; }
        public DateTime? ErrorDateMastodon { get; set; }
        public DateTime? ErrorDateTwitter { get; set; }
        public TimeSpan Delay { get; set; }
        public long LastToot { get; set; }
        public DateTime? LastFollowerSearch { get; set; }
    }
}
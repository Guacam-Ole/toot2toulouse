using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend.Configuration
{
    public class UserConfiguration
    {

        public ITwitter.LongContent LongContent { get; set; } // What to do if toot is longer than allowed tweet
        public TootConfigurationAppThreadOptions LongContentThreadOptions { get; set; } // prefix and suffix when splitting toot into thread
        public Dictionary<string, string> Replacements { get; set; } // Autoreplacements for words (e.g. toot->tweet)
        public UserConfigurationFollowers Followers { get; set; } // followersearch settings (translate mastodon mentions to twitter mentions)
        public UserConfigurationAppSuffix AppSuffix { get; set; } // suffix to show on tweets
    }

    public class UserConfigurationFollowers
    {
        public List<ITwitter.Followersearch> Search { get; set; }
        public UserConfigurationFollowersText FollowerText { get; set; }

    }

    public class UserConfigurationFollowersText
    {
        public string Prefix { get; set; }
        public bool HideInstance { get; set; }
    }

    public class TootConfigurationAppThreadOptions
    {
        public string Prefix { get; set; }
        public string Suffix { get; set; }
    }

    public class UserConfigurationAppSuffix
    {
        public string Content { get; set; }
        public bool HideOnLongText { get; set; }
    }
}

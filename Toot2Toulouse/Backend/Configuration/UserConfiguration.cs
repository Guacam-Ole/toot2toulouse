namespace Toot2Toulouse.Backend.Configuration
{
    public class UserConfiguration
    {
        public Dictionary<Mastodon.Visibilites, Twitter.Visibilities> Visibility { get; set; }
        public Twitter.ContentWarnings ContentWarning { get; set; }
        public Twitter.Replies Replies { get; set; }
        public Twitter.LongContent LongContent { get; set; }
        public TootConfigurationAppThreadOptions LongContentThreadOptions { get; set; }
        public Dictionary<string, string> Replacements { get; set; }
        public UserConfigurationFollowers Followers { get; set; }
        public UserConfigurationAppSuffix AppSuffix { get; set; }
    }

    public class UserConfigurationFollowers
    {
        public List<Twitter.Followersearch> Search { get; set; }
        public UserConfigurationWollowersText FollowerText { get; set; }

    }

    public class UserConfigurationWollowersText
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

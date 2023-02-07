﻿using System.Text.Json.Serialization;

using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend.Configuration
{
    public class UserConfiguration
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Visibilities
        {
            Public,
            Unlisted,
            Private,
            Direct
        }


        [OverviewCategory("Long Tweets", "If toot is longer than an allowed tweet")]
        public ITwitter.LongContent LongContent { get; set; } // What to do if toot is longer than allowed tweet
        public TootConfigurationAppThreadOptions LongContentThreadOptions { get; set; } // prefix and suffix when splitting toot into thread
        public Dictionary<string, string> Replacements { get; set; } // Autoreplacements for words (e.g. toot->tweet)
        public UserConfigurationFollowers Followers { get; set; } // followersearch settings (translate mastodon mentions to twitter mentions)

        public UserConfigurationAppSuffix AppSuffix { get; set; } // suffix to show on tweets
        public List<string> DontTweet { get; set; } = new List<string>() { { "🐘"  } };
        public TimeSpan Delay { get; set; }
    

        public List<Visibilities> VisibilitiesToPost { get; set; }
    }



    public class UserConfigurationFollowers
    {
        //public List<ITwitter.Followersearch> Search { get; set; }
        public UserConfigurationFollowersText FollowerText { get; set; }

    }

    public class UserConfigurationFollowersText
    {
        [OverviewCategory("Mentions", "Suffix to show before mentioned users")]
        public string Prefix { get; set; }
        [OverviewCategory("Mentions", "Hide instance name (e.g. \"🐘ole\" instead of \"🐘ole@mastodon.social\") ")]
        public bool HideInstance { get; set; }
    }

    public class TootConfigurationAppThreadOptions
    {
        [OverviewCategory("Long Tweets", "Prefix to add on replies in a thread")]
        public string Prefix { get; set; }
        [OverviewCategory("Long Tweets", "Suffix to add on replies in a thread")]
        public string Suffix { get; set; }
    }

    public class UserConfigurationAppSuffix
    {
        [OverviewCategory("Crossposter", "Suffix to show after Tweet (To inform others this is a crossposter-tweet)")]
        public string Content { get; set; }
        [OverviewCategory("Crossposter", "Remove suffix when this would cause the tweet to be too long")]
        public bool HideIfBreaks { get; set; }
    }
}

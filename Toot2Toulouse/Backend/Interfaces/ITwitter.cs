using Toot2Toulouse.Backend.Configuration;

using Tweetinvi;
using Tweetinvi.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        public enum Visibilities
        {
            PublicAll,
            PublicFollowers,
            PublicMentionedHide,
            PublicMentionedShow,
            Circle,
            DontPublish
        }

        public enum ContentWarnings
        {
            DontPublish,
            NoCw,
            NoCwSensitive,
            WithCw,
            WithCwSensitive,
        }

        public enum Replies
        {
            Publish,
            DontPublish,
            Thread
        }

        public enum LongContent
        {
            Cut,
            CutLink,
            DontPublish,
            Thread
        }

        public enum Followersearch
        {
            TwitterContactName,
            TwitterContactDescription,
            PersonalTranslation,
            GlobalTranslation,
            Text,
            Backlink
        }

        Task<ITweet> TweetAsync(string content, long? replyTo = null); // TODO: Media, Mentions

        Task<string> GetAuthenticationUrlAsync(string baseUrl);

        Task<bool> FinishAuthenticationAsync(string query);

        Task InitUserAsync(TwitterClient userClient, UserConfiguration userConfiguration);

        Task PublishAsync(Mastonet.Entities.Status toot);
    }
}
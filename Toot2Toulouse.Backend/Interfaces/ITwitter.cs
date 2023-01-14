using System.ComponentModel.DataAnnotations;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Models;

using Tweetinvi;
using Tweetinvi.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        public enum LongContent
        {
            [Display(Name = "Cut the tweet")]
            Cut,

            [Display(Name = "Cut the tweet and add link to toot")]
            CutLink,

            [Display(Name = "Don't publish at all")]
            DontPublish,

            [Display(Name = "Create a thread")]
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

        Task<ITweet> TweetAsync(string content, bool isSensitive, long replyTo);

        Task<string> GetAuthenticationUrlAsync(string baseUrl);

        Task<bool> FinishAuthenticationAsync(string query);

        Task InitUserAsync(UserData userData);

        Task PublishAsync(Mastonet.Entities.Status toot);
    }
}
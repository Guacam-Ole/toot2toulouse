
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using Toot2Toulouse.Backend.Models;

using Tweetinvi.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum LongContent
        {
      //      [Display(Name = "Cut the tweet")]
            Cut,

        //    [Display(Name = "Cut the tweet and add link to toot")]
            CutLink,

       //     [Display(Name = "Don't publish at all")]
            DontPublish,

         //   [Display(Name = "Create a thread")]
            Thread
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum Followersearch
        {
            TwitterContactName,
            TwitterContactDescription,
            PersonalTranslation,
            GlobalTranslation,
            Text,
            Backlink
        }

        //Task<ITweet> TweetAsync(UserData userData, string content, bool isSensitive, long replyTo);

        //Task<string> GetAuthenticationUrlAsync(string baseUrl);

        //Task<bool> FinishAuthenticationAsync(string query);

        //Task InitUserAsync(UserData userData);

        Task<List<long>> PublishAsync(UserData userData, Mastonet.Entities.Status toot);
    }
}
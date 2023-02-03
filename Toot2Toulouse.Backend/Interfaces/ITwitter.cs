using System.Text.Json.Serialization;

using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum LongContent
        {
            Cut,
            CutLink,
            DontPublish,
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

        Task<List<long>> PublishAsync(UserData userData, Mastonet.Entities.Status toot, long? replyTo = null);
    }
}
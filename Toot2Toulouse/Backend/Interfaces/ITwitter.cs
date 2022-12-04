using Tweetinvi;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface ITwitter
    {
        //Task AuthTest();


        Task<bool> Tweet(TwitterClient userClient, string content); // TODO: Media, Mentions

        Task<string> GetAuthenticationUrl(string baseUrl);

        Task<bool> FinishAuthentication(string query);
    }
}

using Toot2Toulouse.Backend.Interfaces;

using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Parameters;

namespace Toot2Toulouse.Backend
{
    public class Twitter : ITwitter
    {
        private Secrets _secrets;
        private TwitterClient _appClient;

        private static readonly IAuthenticationRequestStore _twitterRequestStore = new LocalAuthenticationRequestStore();

        public Twitter(ITootConfiguration tootConfiguration)
        {
            _secrets = tootConfiguration.GetSecrets();
            _appClient = new TwitterClient(_secrets.Twitter.Consumer.ApiKey, _secrets.Twitter.Consumer.ApiKeySecret);
        }


        public async Task<string> GetAuthenticationUrl(string baseUrl)
        {
            var guid=Guid.NewGuid();
            var targetUrl = baseUrl + "/TwitterAuth";
            var redirectUrl = _twitterRequestStore.AppendAuthenticationRequestIdToCallbackUrl(targetUrl, guid.ToString());
            var authTokenRequest=await _appClient.Auth.RequestAuthenticationUrlAsync(redirectUrl);
            await _twitterRequestStore.AddAuthenticationTokenAsync(guid.ToString(), authTokenRequest);
            return authTokenRequest.AuthorizationURL;
        }

        private void TestAuthenticate()
        {
            _appClient = new TwitterClient(_secrets.Twitter.Consumer.ApiKey, _secrets.Twitter.Consumer.ApiKeySecret, _secrets.Twitter.Personal.AccessToken, _secrets.Twitter.Personal.AccessTokenSecret);
        }

        //public async Task AuthTest()
        //{
        //    TestAuthenticate();
        //    var user = await _appClient.Users.GetAuthenticatedUserAsync();
        //    Console.WriteLine($"Moinsen, {user}");




        //    string suffix = " [🐘²🐦]";
        //    await Tweet("Nur ein simpler Testtweet via API. Einfach ignorieren.  " + suffix);
        //}



        public async Task<bool> Tweet(TwitterClient userClient, string content)
        {
            try
            {
                var response = await userClient.Tweets.PublishTweetAsync(content);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> FinishAuthentication(string query)
        {
            var requestParameters = await RequestCredentialsParameters.FromCallbackUrlAsync(query, _twitterRequestStore);
            var userCredentials=await _appClient.Auth.RequestCredentialsAsync(requestParameters);

            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();

            string suffix = " [🐘²🐦]";
            return await Tweet(userClient, "Nur ein simpler Testtweet via API. Einfach ignorieren :)   " + suffix);
        }
    }
}

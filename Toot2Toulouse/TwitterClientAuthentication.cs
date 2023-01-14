using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Interfaces;

using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Parameters;

namespace Toot2Toulouse
{
    public class TwitterClientAuthentication:ITwitterClientAuthentication
    {
        private readonly ILogger<TwitterClientAuthentication> _logger;
        private readonly IDatabase _database;
        private readonly INotification _notification;
        private readonly ICookies _cookies;
        private readonly TootConfiguration _config;
        private static readonly IAuthenticationRequestStore _twitterRequestStore = new LocalAuthenticationRequestStore();

        public TwitterClientAuthentication(ILogger<TwitterClientAuthentication> logger, IDatabase database, INotification notification, ICookies cookies, ConfigReader configReader)
        {
            _logger = logger;
            _database = database;
            _notification = notification;
            _cookies = cookies;
            _config = configReader.Configuration;
        }

        private TwitterClient GetAppClient()
        {
            return new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
        }

        public async Task<bool> FinishAuthenticationAsync(string query)
        {
            var hash = _cookies.UserHashGetCookie();
            var id = _cookies.UserIdGetCookie();

            if (id == Guid.Empty || hash == null) throw new Exception("No cookie. You shouldn't even be here");

            var t2tUser = _database.GetUserByIdAndHash(id, hash);
            if (t2tUser == null) throw new Exception("invalid cookie data");

            var requestParameters = await RequestCredentialsParameters.FromCallbackUrlAsync(query, _twitterRequestStore);
            var userCredentials = await GetAppClient().Auth.RequestCredentialsAsync(requestParameters);

            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();
            t2tUser.Twitter = new Backend.Models.Twitter
            {
                ConsumerKey = userCredentials.ConsumerKey,
                ConsumerSecret = userCredentials.ConsumerSecret,
                AccessToken = userCredentials.AccessToken,
                AccessSecret = userCredentials.AccessTokenSecret,
                Bearer = userCredentials.BearerToken,
                Id = user.Id.ToString(),
                DisplayName = user.Name,
                Handle = user.ScreenName
            };
            _database.UpsertUser(t2tUser);

            _notification.Info(t2tUser.Id, TootConfigurationApp.MessageCodes.RegistrationFinished);

            //   InitUserAsync(t2tUser);
            return true;
        }

        public async Task<string> GetAuthenticationUrlAsync(string baseUrl)
        {
            try
            {
                var guid = Guid.NewGuid();
                var targetUrl = baseUrl + "/twitter/code";
                var redirectUrl = _twitterRequestStore.AppendAuthenticationRequestIdToCallbackUrl(targetUrl, guid.ToString());
                var authTokenRequest = await GetAppClient().Auth.RequestAuthenticationUrlAsync(redirectUrl);
                await _twitterRequestStore.AddAuthenticationTokenAsync(guid.ToString(), authTokenRequest);
                return authTokenRequest.AuthorizationURL;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed retrieving AuthenticationUrl", ex);
                throw;
            }
        }
    }
}
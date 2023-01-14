using Mastonet;
using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

namespace Toot2Toulouse.Backend
{
    public class Mastodon : IMastodon
    {
        private readonly ILogger<Mastodon> _logger;
        //private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IDatabase _database;
        private readonly ICookies _cookies;
        private readonly TootConfiguration _configuration;
        private readonly Dictionary<MessageCodes, string> _messages;

        public Mastodon(ILogger<Mastodon> logger, ConfigReader configuration,  IDatabase database, ICookies cookies)
        {
            _logger = logger;
            //_webHostEnvironment = webHostEnvironment;
            _database = database;
            _cookies = cookies;
            _configuration = configuration.Configuration;
            _messages = configuration.GetMessagesForLanguage(_configuration.App.DefaultLanguage);   // TODO: Allow per-user Language setting
        }

        public async Task SendStatusMessageTo(Guid id, string? prefix, MessageCodes messageCode)
        {
            var user = _database.GetUserById(id);
            string recipient = "@"+ user.Mastodon.Handle + "@" + user.Mastodon.Instance;
            await ServiceToot($"{recipient}\n{prefix}{_messages[messageCode]}{_configuration.App.ServiceAppSuffix}", Visibility.Direct);
            _logger.LogInformation("Sent Statusmessage {messageCode} to {recipient}", messageCode, recipient);
        }

        public async Task<string> GetAuthenticationUrl(string requestHost, string userInstance)
        {
            var serviceClient = await GetAuthenticationClient(userInstance, true);
            var url = serviceClient.OAuthUrl();
            return url;
        }

        public async Task<AuthenticationClient> GetAuthenticationClient(string userInstance, bool createApp)
        {
            var authClient = new AuthenticationClient(userInstance);
            if (createApp)
            {
                var appRegistration = await authClient.CreateApp(_configuration.App.ClientName, Scope.Read);
                _cookies.AppRegistrationSetSession(appRegistration);
            }
            else
            {
                authClient.AppRegistration = _cookies.AppRegistrationGetSession();
            }

            return authClient;
        }

        private async Task<string> GetUserAccessTokenByCode(string instance, string code)
        {
            try
            {
                var authClient = await GetAuthenticationClient(instance, false);
                var auth = await authClient.ConnectWithCode(code);
                return auth.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "cannot get authtoken by code on instance {instance}", instance);
                throw;
            }
        }

        private async Task<Account?> ConnectUserAccountByAuthToken(string instance, string accessToken)
        {
            try
            {
                return await GetUserAccountByAccessToken(instance, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error trying to connect by code to '{instance}'", instance);
                throw;
            }
        }

        private async Task<Account?> GetUserAccountByAccessToken(string instance, string accessToken)
        {
            try
            {
                var serviceClient = new MastodonClient(instance, accessToken);
                if (serviceClient == null) return null;

                return await serviceClient.GetCurrentUser();
            }
            catch (ServerErrorException mastoEx)
            {
                _logger.LogWarning(mastoEx, "Mastodon Auth error on '{instance}'", instance);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected Mastodon error on '{instance}'", instance);
                return null;
            }
        }

        private void StoreNewUser(string instance, string accessToken, Account userAccount)
        {
            var user = new UserData
            {
                Config = _configuration.Defaults,
                Id = Guid.NewGuid(),
                Mastodon = new Models.Mastodon
                {
                    Id = userAccount.Id,
                    DisplayName= userAccount.DisplayName,   
                    Handle = userAccount.AccountName,
                    Instance = instance,
                    Secret = accessToken
                }
            };
            _database.UpsertUser(user, true);
            string hash = _database.CalculateHashForUser(user);

            _cookies.UserIdSetCookie(user.Id);
            _cookies.UserHashSetCookie(hash);
        }

        public async Task<KeyValuePair<bool, string>> UserIsAllowedToRegister(string userInstance, string verificationCode)
        {
            try
            {
                var authToken = await GetUserAccessTokenByCode(userInstance, verificationCode);

                var userAccount = await ConnectUserAccountByAuthToken(userInstance, authToken);
                if (userAccount == null)
                    return new KeyValuePair<bool, string>(false, "authorization failed");

                if (!_configuration.App.Modes.AllowBots && (userAccount.Bot ?? false))
                    return new KeyValuePair<bool, string>(false, "Bots are not allowed on this server");
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.AllowedInstances) && !_configuration.App.Modes.AllowedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    return new KeyValuePair<bool, string>(false, $"Only users from the following instances are allowed currently: {_configuration.App.Modes.AllowedInstances}");
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.BlockedInstances) && _configuration.App.Modes.BlockedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    return new KeyValuePair<bool, string>(false, "Your Instance is blocked");

                // TODO: Check maxTootsPerDay

                StoreNewUser(userInstance, authToken, userAccount);

                return new KeyValuePair<bool, string>(true, "success");
            }
            catch (Exception ex)
            {
                return new KeyValuePair<bool, string>(false, $"Server Error: {ex}");
            }
        }

        private MastodonClient GetServiceClient()
        {
            try
            {
                var serviceClient = new MastodonClient(_configuration.App.Instance, _configuration.Secrets.Mastodon.AccessToken);
                _logger.LogDebug("Successfully retrieved Serviceclient for {Instance} using accesstoken", _configuration.App.Instance);
                return serviceClient;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot retrieve serviceclient for {Instance} using accesstoken", _configuration.App.Instance);
                throw;
            }
        }

        public async Task ServiceToot(string content, Visibility visibility)
        {
            try
            {
                var mastodonClient = GetServiceClient();
                await mastodonClient.PublishStatus(content, visibility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending Status Message: '{content}'", content);
            }
        }

        public async Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100)
        {
            var mastodonClient = GetServiceClient();
            var serviceUser = await mastodonClient.GetCurrentUser();
            var userName = serviceUser.UserName;
            var statuses = await mastodonClient.GetAccountStatuses(serviceUser.Id, new ArrayOptions { Limit = limit });
            var matches = statuses.Where(q => q.Content.Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
            _logger.LogDebug("Found {matchtes} matches when searching for '{searchString}' in service User", matches.Count(), searchString);
            return matches;
        }
    }
}
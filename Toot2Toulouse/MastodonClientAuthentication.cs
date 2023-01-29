using Mastonet;
using Mastonet.Entities;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;
using Toot2Toulouse.Interfaces;

namespace Toot2Toulouse
{
    public class MastodonClientAuthentication : IMastodonClientAuthentication
    {
        private readonly TootConfiguration _configuration;
        private readonly ILogger<MastodonClientAuthentication> _logger;
        private readonly IDatabase _database;
        private readonly ICookies _cookies;
        private readonly IMastodon _mastodon;
        private readonly IToulouse _toulouse;

        public MastodonClientAuthentication(ILogger<MastodonClientAuthentication> logger, IDatabase database, ICookies cookies, ConfigReader configReader, IMastodon mastodon, IToulouse toulouse)
        {
            _configuration = configReader.Configuration;
            _logger = logger;
            _database = database;
            _cookies = cookies;
            _mastodon = mastodon;
            _toulouse = toulouse;
        }

        public async Task<KeyValuePair<bool, string>> UserIsAllowedToRegister(string userInstance, string verificationCode)
        {
            try
            {
                var authToken = await GetUserAccessTokenByCode(userInstance, verificationCode);
                var userData = new UserData
                {
                    Mastodon = new Backend.Models.Mastodon { Instance = userInstance, Secret = authToken },
                    Twitter=new Backend.Models.Twitter ()
                };

                var userAccount = await _mastodon.GetUserAccount(userData);
                if (userAccount == null)
                    return new KeyValuePair<bool, string>(false, "authorization failed");

                if (!_configuration.App.Modes.AllowBots && (userAccount.Bot ?? false))
                    return new KeyValuePair<bool, string>(false, "Bots are not allowed on this server");
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.AllowedInstances) && !_configuration.App.Modes.AllowedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    return new KeyValuePair<bool, string>(false, $"Only users from the following instances are allowed currently: {_configuration.App.Modes.AllowedInstances}");
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.BlockedInstances) && _configuration.App.Modes.BlockedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    return new KeyValuePair<bool, string>(false, "Your Instance is blocked");
                if (_toulouse.GetServerMode() == TootConfigurationAppModes.ValidModes.Closed)
                    return new KeyValuePair<bool, string>(false, "This server isn't accepting new registrations. (I thought we already told you that?)");
                if (_configuration.App.Modes.Active == TootConfigurationAppModes.ValidModes.Invite)
                {
                    var invites = await _mastodon.GetServiceTootsContaining("[INVITE]", 100, $"{userAccount.AccountName}@{userInstance}");
                    if (invites.Count == 0)
                        return new KeyValuePair<bool, string>(false, "Looks like you did not receive an invite or your invite has expired");
                }

                // TODO: Check maxTootsPerDay

                StoreNewUser(userData, userAccount);
                await _mastodon.AssignLastTweetedIfMissing(userData.Id);

                return new KeyValuePair<bool, string>(true, "success");
            }
            catch (Exception ex)
            {
                return new KeyValuePair<bool, string>(false, $"Server Error: {ex}");
            }
        }

        private void StoreNewUser(UserData user, Account userAccount)
        {
            var oldUserId = _database.GetUserIdByMastodonId(user.Mastodon.Instance, userAccount.Id);
            if (oldUserId != null)
            {
                var secret = user.Mastodon.Secret;
                user = _database.GetUserById(oldUserId.Value);
                user.Mastodon.Secret = secret;
            }
            else
            {
                user.Config = _configuration.Defaults;
                user.Mastodon.LastTootDate = userAccount.LastStatusAt;
            }
            user.Mastodon.Id = userAccount.Id;
            user.Mastodon.DisplayName = userAccount.DisplayName;
            user.Mastodon.Handle = userAccount.AccountName;
         
            _database.UpsertUser(user);
            string hash = _database.CalculateHashForUser(user);

            _cookies.UserIdSetCookie(user.Id);
            _cookies.UserHashSetCookie(hash);
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

        public async Task<string> GetAuthenticationUrl(string requestHost, string userInstance)
        {
            var serviceClient = await GetAuthenticationClient(userInstance, true);
            var url = serviceClient.OAuthUrl();
            return url;
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
    }
}
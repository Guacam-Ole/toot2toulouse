using Mastonet;
using Mastonet.Entities;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;
using Toot2Toulouse.Interfaces;

using Toot2ToulouseWeb;

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

        public async Task UserIsAllowedToRegisterAsync(string userInstance, string verificationCode)
        {
            try
            {
                var authToken = await GetUserAccessTokenByCodeAsync(userInstance, verificationCode);

                var userAccount = await _mastodon.GetUserAccountAsync(
                    new UserData
                    {
                        Mastodon = new Backend.Models.Mastodon { Instance = userInstance, Secret = authToken }
                    }
                    );
                if (userAccount == null)
                    throw new ApiException(ApiException.ErrorTypes.Auth);
                if (!_configuration.App.Modes.AllowBots && (userAccount.Bot ?? false))
                    throw new ApiException(ApiException.ErrorTypes.RegistrationNoBots, "Bots are not allowed on this server", 403);
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.AllowedInstances) && !_configuration.App.Modes.AllowedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    throw new ApiException(ApiException.ErrorTypes.RegistrationWrongInstance, $"Only users from the following instances are allowed currently: {_configuration.App.Modes.AllowedInstances}", 403);
                if (!string.IsNullOrWhiteSpace(_configuration.App.Modes.BlockedInstances) && _configuration.App.Modes.BlockedInstances.Contains(userInstance, StringComparison.InvariantCultureIgnoreCase))
                    throw new ApiException(ApiException.ErrorTypes.RegistrationWrongInstance, "Your Instance is blocked", 403);
                if (_toulouse.GetServerMode() == TootConfigurationAppModes.ValidModes.Closed)
                    throw new ApiException(ApiException.ErrorTypes.RegistrationClosed, "This server isn't accepting new registrations. (I thought we already told you that?)", 403);
                if (_configuration.App.Modes.Active == TootConfigurationAppModes.ValidModes.Invite)
                {
                    var invites = await _mastodon.GetServiceTootsContainingAsync("[INVITE]", 100, $"{userAccount.AccountName}@{userInstance}");
                    if (invites.Count == 0)
                        throw new ApiException(ApiException.ErrorTypes.RegistrationInvite, "Looks like you did not receive an invite or your invite has expired", 403);
                }

                // TODO: Check maxTootsPerDay

                StoreNewUser(userInstance, authToken, userAccount);
            }
            catch (ApiException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ApiException(ApiException.ErrorTypes.Exception, ex.Message);
            }
        }

        private void StoreNewUser(string instance, string secret, Account userAccount)
        {
            UserData user = null;
            var existiungUserId = _database.GetUserIdByMastodonId(instance, userAccount.Id);
            if (existiungUserId != null)
            {
                user = _database.GetUserById(existiungUserId.Value);
            }
            else
            {
                user = new UserData
                {
                    Config = _configuration.Defaults
                };
            }

            user.Mastodon.Secret = secret;
            user.Mastodon.Instance = instance;
            user.Mastodon.LastTootDate = userAccount.LastStatusAt;
            user.Mastodon.Id = userAccount.Id;
            user.Mastodon.DisplayName = userAccount.DisplayName;
            user.Mastodon.Handle = userAccount.AccountName;

            if (user.BlockReason == UserData.BlockReasons.AuthMastodon)
            {
                user.BlockReason = null;
                user.BlockDate = null;
            }

            _database.UpsertUser(user);
            string hash = _database.CalculateHashForUser(user);

            _cookies.UserIdSetCookie(user.Id);
            _cookies.UserHashSetCookie(hash);
        }

        public async Task<AuthenticationClient> GetAuthenticationClientAsync(string userInstance, bool createApp)
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

        public async Task<string> GetAuthenticationUrlAsync(string requestHost, string userInstance)
        {
            var serviceClient = await GetAuthenticationClientAsync(userInstance, true);
            var url = serviceClient.OAuthUrl();
            return url;
        }

        private async Task<string> GetUserAccessTokenByCodeAsync(string instance, string code)
        {
            try
            {
                var authClient = await GetAuthenticationClientAsync(instance, false);
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
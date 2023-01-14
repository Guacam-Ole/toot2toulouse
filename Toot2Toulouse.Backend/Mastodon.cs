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
        private readonly TootConfiguration _configuration;
        private readonly Dictionary<MessageCodes, string> _messages;

        public Mastodon(ILogger<Mastodon> logger, ConfigReader configuration,  IDatabase database)
        {
            _logger = logger;
            //_webHostEnvironment = webHostEnvironment;
            _database = database;
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


        //public async Task<Account?> ConnectUserAccountByAuthToken(string instance, string accessToken)
        //{
        //    try
        //    {
        //        return await GetUserAccountByAccessToken(instance, accessToken);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogDebug(ex, "Error trying to connect by code to '{instance}'", instance);
        //        throw;
        //    }
        //}

        public async Task<Account?> GetUserAccountByAccessToken(string instance, string accessToken)
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
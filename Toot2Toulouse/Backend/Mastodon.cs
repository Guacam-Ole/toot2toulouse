using Mastonet;
using Mastonet.Entities;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

namespace Toot2Toulouse.Backend
{
    public class Mastodon : IMastodon
    {
        private readonly ILogger<Mastodon> _logger;
        private readonly TootConfiguration _configuration;
        private readonly Dictionary<MessageCodes, string> _messages;

        public Mastodon(ILogger<Mastodon> logger, ConfigReader configuration)
        {
            _logger = logger;
            _configuration = configuration.Configuration;
            _messages = configuration.GetMessagesForLanguage(_configuration.App.DefaultLanguage);   // TODO: Allow per-user Language setting
        }

        private async Task SendStatusMessageTo(string recipient, MessageCodes messageCode)
        {
            await ServiceToot($"{recipient}\n{_messages[messageCode]}{_configuration.App.ServiceAppSuffix}", Visibility.Direct);
        }

        public async Task SendAllStatusMessagesToAsync(string recipient)
        {
            foreach (var messageCode in Enum.GetValues<MessageCodes>())
            {
                await SendStatusMessageTo(recipient, messageCode);
            }
        }

        private MastodonClient GetServiceClient()
        {
            try
            {
                var serviceClient=new MastodonClient(_configuration.App.Instance, _configuration.Secrets.Mastodon.AccessToken);
                _logger.LogDebug($"Successfully retrieved Serviceclient for {_configuration.App.Instance} using accesstoken");
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
                _logger.LogError( ex, "Failed sending Status Message: '{content}'",content);
            }
        }

        public async Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100)
        {
            var mastodonClient = GetServiceClient();
            var serviceUser = await mastodonClient.GetCurrentUser();
            var userName = serviceUser.UserName;
            var statuses=await mastodonClient.GetAccountStatuses(serviceUser.Id, new ArrayOptions {  Limit=limit});
            var matches = statuses.Where(q => q.Content.Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
            _logger.LogDebug($"Found {matches.Count()} matches when searching for '{searchString}' in service User");
            return matches;
        }
    }
}
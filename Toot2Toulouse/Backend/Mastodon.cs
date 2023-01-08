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

        public Mastodon(ILogger<Mastodon> logger, ConfigReader configuration)
        {
            _logger = logger;
            _configuration = configuration.Configuration;
        }

        private async Task SendStatusMessageTo(string recipient, MessageCodes messageCode)
        {
            await ServiceToot($"{recipient}\n{_configuration.App.Messages[messageCode]}{_configuration.App.ServiceAppSuffix}", Visibility.Direct);
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
                _logger.LogCritical($"Cannot retrieve serviceclient for {_configuration.App.Instance} using accesstoken", ex);
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
                _logger.LogError($"Failed sending Status Message: '{content}'", ex);
            }
        }

        public async Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100)
        {
            var mastodonClient = GetServiceClient();
            var serviceUser = await mastodonClient.GetCurrentUser();
            var userName = serviceUser.UserName;
            var timeline = await mastodonClient.GetHomeTimeline(new ArrayOptions { Limit = limit });
            var matches = timeline.Where(q => q.Content.Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
            _logger.LogDebug($"Found {matches.Count()} matches when searching for '{searchString}' in service User");
            return matches;
        }
    }
}
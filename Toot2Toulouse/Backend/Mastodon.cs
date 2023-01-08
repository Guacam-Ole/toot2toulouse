using Mastonet;
using Mastonet.Entities;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

namespace Toot2Toulouse.Backend
{
    public class Mastodon:IMastodon
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
            return new MastodonClient(_configuration.App.Instance, _configuration.Secrets.Mastodon.AccessToken);
        }

        public async Task ServiceToot(string content, Visibility visibility)
        {
            var mastodonClient = GetServiceClient();
            await mastodonClient.PublishStatus(content, visibility);
        }

        public async Task<IEnumerable<Status>> GetPostsContainingAsync(string searchString, int limit=100)
        {
            var mastodonClient = GetServiceClient();
            var serviceUser = await mastodonClient.GetCurrentUser();
            var userName = serviceUser.UserName;
            var timeline=await mastodonClient.GetHomeTimeline(new ArrayOptions { Limit=limit});
             return timeline.Where(q=>q.Content.Contains(searchString,StringComparison.InvariantCultureIgnoreCase));
        }

        


        //public async Task ServiceToot(TootConfigurationApp appConfig, SecretsMastodon mastodonSecrets)
        //{
        //    var authClient = new AuthenticationClient(new AppRegistration { ClientId = mastodonSecrets.ClientId, ClientSecret = mastodonSecrets.ClientSecret, Instance = appConfig.Instance, Scope = Scope.Read | Scope.Write | Scope.Follow });
        //    var reg = authClient.AppRegistration;

        //    //var connect = await authClient.ConnectWithCode(mastodonSecrets.AccessToken);
        //    var client=new MastodonClient(appConfig.Instance, mastodonSecrets.AccessToken);
        //    var account=await client.GetCurrentUser();
        //    await client.PublishStatus("Nur ein Test an @stammtischphilosoph@chaos.social ");
        //}

        public async Task<SecretsMastodon> CreateNewAppAsync(TootConfigurationApp appConfig, SecretsMastodon mastodonSecrets)
        {
            if (!string.IsNullOrEmpty(mastodonSecrets.ClientId) || !string.IsNullOrEmpty(mastodonSecrets.ClientSecret))
            {
                throw new ArgumentException("Application already authenticated. See Readme if you think this is wrong");
            }

            if (string.IsNullOrEmpty(appConfig.Url) || string.IsNullOrEmpty(appConfig.ClientName) || string.IsNullOrEmpty(appConfig.Instance))
            {
                throw new ArgumentException("Appconfig missing.");
            }

            // Let's try to create a mew Application on instance:
            try
            {
                var authClient = new AuthenticationClient(appConfig.Instance);

                var mastoddonApp = await authClient.CreateApp(appConfig.ClientName, Scope.Read | Scope.Write | Scope.Follow, appConfig.Url);

                //var client= new AuthenticationClient(mastoddonApp);

                //mastodonSecrets.ClientId= mastoddonApp.ClientId;
                //mastodonSecrets.ClientSecret= mastoddonApp.ClientSecret;

                //authClient.CreateApp()

                return mastodonSecrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when trying to create app");
                throw;
            }
        }
    }
}
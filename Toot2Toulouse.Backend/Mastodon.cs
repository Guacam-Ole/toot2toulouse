using Mastonet;
using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.Messages;

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
            _messages = configuration.GetMessagesForLanguage(_configuration.App.Languages.Default);   // TODO: Allow per-user Language setting
        }

        public async Task SendStatusMessageToAsync(UserData? user, string? prefix, MessageCodes messageCode, string? additionalInfo)
        {
            string? recipient = user == null ? null : "@" + user.Mastodon.Handle + "@" + user.Mastodon.Instance;

            string message = $"{recipient}\n{prefix}{_messages[messageCode]}\n{additionalInfo}\n{_configuration.App.AppInfo.Suffix}";
            ReplaceServiceTokens(ref message);
            await ServiceTootAsync(message, Visibility.Direct);
            _logger.LogInformation("Sent Statusmessage {messageCode} to {recipient}", messageCode, recipient);
        }

        private void ReplaceServiceTokens(ref string message)
        {
            if (message == null) return;
            var replacements = new Dictionary<string, string>();
            GetConfigValues(_configuration, string.Empty, replacements);
            foreach (var replacement in replacements)
            {
                message = message.Replace($"[{replacement.Key}]", replacement.Value);
            }
        }

        private void GetConfigValues<T>(T root, string prefix, Dictionary<string, string> displayProperties)
        {
            if (root == null) return;
            //     if (prefix.Contains("Secret")) return;

            var properties = root.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType.Name.Contains("Secret")) continue;
                if (!propertyInfo.CanRead) continue;
                var value = propertyInfo.GetValue(root);

                if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType.Namespace.StartsWith("Toot2Toulouse"))
                {
                    GetConfigValues(value, prefix + propertyInfo.Name + ".", displayProperties);
                }
                var stringValue = string.Empty;
                if (value != null) stringValue = value.ToString();
                displayProperties.Add(prefix + propertyInfo.Name, stringValue);
            }
        }

        private MastodonClient GetUserClientByAccessToken(string instance, string accessToken)
        {
            return new MastodonClient(instance, accessToken);
        }

        public MastodonClient GetUserClient(UserData userData)
        {
            return GetUserClientByAccessToken(userData.Mastodon.Instance, userData.Mastodon.Secret);
        }

        public async Task<Account?> GetUserAccountAsync(UserData userData)
        {
            var userClient = GetUserClient(userData);
            return await userClient.GetCurrentUser();
        }

        public async Task<Account?> GetUserAccountAsync(MastodonClient mastodonClient)
        {
            return await mastodonClient.GetCurrentUser();
        }

        public async Task<Account?> GetUserAccountAsync(string instance, string accessToken)
        {
            return await GetUserAccountAsync(new UserData { Mastodon = new Models.Mastodon { Instance = instance, Secret = accessToken } });
        }

        public async Task AssignLastTweetedIfMissingAsync(UserData user)
        {
            if (user.Mastodon.LastToot != null) return;
            var client = GetUserClient(user);

            var lastStatuses = await client.GetAccountStatuses(user.Mastodon.Id, new ArrayOptions { Limit = 1 }, false, true, false, true);
            var lastTweeted = lastStatuses.OrderBy(q => q.CreatedAt).FirstOrDefault();
            user.Mastodon.LastToot = lastTweeted.Id;
            user.Update = true;
        }

        public async Task<List<Status>> GetNonPostedTootsAsync(UserData user)
        {
            await AssignLastTweetedIfMissingAsync(user);
            var client = GetUserClient(user);
            var statuses = await client.GetAccountStatuses(user.Mastodon.Id, new ArrayOptions { Limit = 1000, SinceId = user.Mastodon.LastToot }, false, true, false, true);
            return statuses.OrderBy(q => q.CreatedAt).ToList();
        }

        public async Task<List<Status>> GetServiceTootsContainingAsync(string content, int limit = 100, string? recipient = null)
        {
            return await GetTootsContainingAsync(GetServiceClient(), content, limit, recipient);
        }

        private async Task<List<Status>> GetTootsContainingAsync(MastodonClient client, string content, int limit = 100, string? recipient = null)
        {
            var statuses = await client.GetAccountStatuses((await client.GetCurrentUser()).Id, new ArrayOptions { Limit = limit }, false, true, false, true);
            var matches = statuses.Where(q => q.Content.Contains(content, StringComparison.InvariantCultureIgnoreCase));
            if (recipient != null)
            {
                matches = matches.Where(q => q.Mentions.Any(m => m.AccountName == recipient));
            }
            return matches.OrderBy(q => q.CreatedAt).ToList();
        }

        public async Task<Status> GetSingleTootAsync(UserData user, string tootId)
        {
            try
            {
                var client = GetUserClient(user);
                return await client.GetStatus(tootId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed retrieving single toot with id {tid} for {uid}", tootId, user.Id);
                throw;
            }
        }

        public async Task<List<Status>> GetTootsContainingAsync(UserData user, string content, int limit = 100)
        {
            try
            {
                var client = GetUserClient(user);
                return await GetTootsContainingAsync(client, content, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "failed searching for toots");
                throw;
            }
        }

        private MastodonClient GetServiceClient()
        {
            try
            {
                var serviceClient = new MastodonClient(_configuration.App.AppInfo.Instance, _configuration.Secrets.Mastodon.AccessToken);
                _logger.LogDebug("Successfully retrieved Serviceclient for {Instance} using accesstoken", _configuration.App.AppInfo.Instance);
                return serviceClient;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Cannot retrieve serviceclient for {Instance} using accesstoken", _configuration.App.AppInfo.Instance);
                throw;
            }
        }

        public async Task ServiceTootAsync(string content, Visibility visibility)
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

        //public async Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100)
        //{
        //    var mastodonClient = GetServiceClient();
        //    var serviceUser = await mastodonClient.GetCurrentUser();
        //    var userName = serviceUser.UserName;
        //    var statuses = await mastodonClient.GetAccountStatuses(serviceUser.Id, new ArrayOptions { Limit = limit });
        //    var matches = statuses.Where(q => q.Content.Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
        //    _logger.LogDebug("Found {matchtes} matches when searching for '{searchString}' in service User", matches.Count(), searchString);
        //    return matches;
        //}
    }
}
using LiteDB;

using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using System.Reflection;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi.Parameters;

namespace Toot2Toulouse.Backend
{
    public class Toulouse : IToulouse
    {
        private readonly ITwitter _twitter;
        private readonly IMastodon _mastodon;
        private readonly IDatabase _database;
        private readonly TootConfiguration _config;
        private readonly ILogger<Toulouse> _logger;

        public Toulouse(ILogger<Toulouse> logger, ConfigReader configReader, ITwitter twitter, IMastodon mastodon, IDatabase database)
        {
            _twitter = twitter;
            _mastodon = mastodon;
            _database = database;
            _config = configReader.Configuration;
            _logger = logger;
        }

        private bool IsSimpleType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsSimpleType(type.GetGenericArguments()[0].GetTypeInfo());
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        private void GetSettingsForDisplayRecursive<T>(T element, string path, List<DisplaySettingsItem> displaySettings)
        {
            var properties = element.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = property.GetValue(element, null);
                    if (!property.PropertyType.IsEnum && property.PropertyType.Namespace != null && property.PropertyType.Namespace.StartsWith("Toot2Toulouse"))
                    {
                        GetSettingsForDisplayRecursive(value, path + "/" + property.Name, displaySettings);
                    }
                    else
                    {
                        var displayAttribute = property.GetCustomAttribute<OverviewCategory>();
                        if (displayAttribute == null) continue;

                        if (value == null && displayAttribute.NullText != null) value = displayAttribute.NullText;

                        if (property.PropertyType.IsEnum)
                        {
                            displaySettings.Add(new DisplaySettingsItem { Category = displayAttribute.Category, DisplayName = displayAttribute.DisplayName ?? property.Name, Path = path + "/" + property.Name, Value = Enum.GetName(property.PropertyType, value), DisplayAsButton = true });
                        }
                        else
                        {
                            displaySettings.Add(new DisplaySettingsItem { Category = displayAttribute.Category, DisplayName = displayAttribute.DisplayName ?? property.Name, Path = path + "/" + property.Name, Value = $"{value}{displayAttribute.Suffix}", DisplayAsButton = property.PropertyType == typeof(bool) });
                        }
                    }
                }
            }
        }

        public List<DisplaySettingsItem> GetServerSettingsForDisplay()
        {
            var displaySettings = new List<DisplaySettingsItem>();
            GetSettingsForDisplayRecursive(_config.App, string.Empty, displaySettings);
            return displaySettings.OrderBy(q => q.Category).ThenBy(q => q.DisplayName).ToList();
        }

        private UserData GetUserByMastodonHandle(string mastodonHandle)
        {
            // TODO: Error handling
            var parts = mastodonHandle.Split('@');
            return _database.GetUserByUsername(parts[0], parts[1]);
        }

        public async Task<List<Status>> GetTootsContaining(string mastodonHandle, string searchstring, int limit)
        {
            var user = GetUserByMastodonHandle(mastodonHandle);
            var toots = await _mastodon.GetTootsContaining(user.Id, searchstring, limit);
            return toots;
        }

        private async Task SendToots(UserData user, List<Status> toots, bool updateUserData)
        {
            var newLastDate = DateTime.UtcNow;
            foreach (var toot in toots)
            {
                // TODO: DELAY

                try
                {
                    if (toot.Id == user.Mastodon.LastToot)
                    {
                        _logger.LogWarning("already tweeted");
                        continue; // already tweeted
                    }
                    if (user.Crossposts.FirstOrDefault(q => q.TootId == toot.Id) != null)
                    {
                        _logger.LogWarning("already tweeted this");
                        continue;
                    }
                    if (toot.InReplyToId != null) continue;

                    var twitterIds = await _twitter.PublishAsync(user, toot);
                    user.Crossposts.Add(new Crosspost { TootId = toot.Id, TwitterIds = twitterIds });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Publishing tweet failed. Will NOT retry");
                }
                user.Mastodon.LastToot = toot.Id;
            }
            user.Mastodon.LastTootDate = newLastDate;
            if (updateUserData) _database.UpsertUser(user);

            _logger.LogDebug("Sent {count} toots to twitter for {user}", toots.Count, user.Mastodon.DisplayName);
        }

        public async Task Invite(string mastodonHandle)
        {
            var user = GetUserByMastodonHandle(mastodonHandle);
            if (user == null)
            {
                _logger.LogWarning("user not found");
                return;
            }

            await _mastodon.SendStatusMessageTo(user.Id, "[INVITE] ", TootConfigurationApp.MessageCodes.Invite);
        }

        public async Task SendTootsForAllUsers()
        {
            var users = _database.GetAllValidUsers().ToList();
            _logger.LogInformation("Sending toots for {count} users", users.Count());
            int totalTootCount = 0;
            foreach (var user in users)
            {
                var notTooted = await _mastodon.GetNonPostedToots(user.Id);
                await SendToots(user, notTooted, true);
            }
            _logger.LogInformation("tweeted {count} toots for all users", totalTootCount);
        }

        public TootConfigurationAppModes.ValidModes GetServerMode()
        {
            var serverMode = _config.App.Modes.Active;
            var serverStats = _database.GetServerStats();

            if (_config.App.Modes.AutoInvite > 0 && serverMode == TootConfigurationAppModes.ValidModes.Closed && _config.App.Modes.AutoInvite <= serverStats.ActiveUsers) serverMode = TootConfigurationAppModes.ValidModes.Invite;
            if (_config.App.Modes.AutoClosed > 0 && _config.App.Modes.AutoClosed <= serverStats.ActiveUsers) serverMode = TootConfigurationAppModes.ValidModes.Closed;

            return serverMode;
        }

        public void CalculateServerStats()
        {
            var serverstats = _database.GetServerStats();
            var allUsers = _database.GetAllValidUsers();
            var activeUsers = allUsers.Where(q => q.Crossposts.Any(q => q.CreatedAt >= DateTime.Now.AddDays(-1)));
            serverstats.ActiveUsers = activeUsers.Count();
            serverstats.TotalUsers = allUsers.Count();
            _database.UpSertServerStats(serverstats);
            _logger.LogDebug("Updated Serverstats.  {serverstats} ", serverstats);
        }
    }
}
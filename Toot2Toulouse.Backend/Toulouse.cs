using LiteDB;

using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using System.Reflection;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi.Core.Models;
using Tweetinvi.Models;

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

        public async Task GetTootsContaining(string mastodonHandle, string searchstring, int limit)
        {
            var parts=mastodonHandle.Split('@');
            var user = _database.GetUserByUsername(parts[0], parts[1]);
            var toots = await _mastodon.GetTootsContaining(user.Id, searchstring, limit);
            await SendToots(user,toots,false); 
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
                    if (toot.CreatedAt < user.Mastodon.LastTootDate)
                    {
                        _logger.LogWarning("toot is older than it should be");
                        continue; // already tweeted
                    }
                    if (toot.InReplyToId != null) continue;
                    if (!user.Config.VisibilitiesToPost.Contains(toot.Visibility)) continue; // wrong visibility
                    var twitterIds = await _twitter.PublishAsync(user, toot);
                    user.Crossposts.Add(new Models.Crosspost { TootId = toot.Id, TwitterIds = twitterIds });
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
    }
}
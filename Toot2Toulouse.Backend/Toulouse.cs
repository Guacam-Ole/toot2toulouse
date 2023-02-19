using LiteDB;

using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using System.Reflection;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi.Exceptions;

using static Toot2Toulouse.Backend.Configuration.Messages;

namespace Toot2Toulouse.Backend
{
    public class Toulouse : IToulouse
    {
        private readonly ITwitter _twitter;
        private readonly IMastodon _mastodon;
        private readonly IDatabase _database;
        private readonly INotification _notification;
        private readonly TootConfiguration _config;
        private readonly ILogger<Toulouse> _logger;

        public Toulouse(ILogger<Toulouse> logger, ConfigReader configReader, ITwitter twitter, IMastodon mastodon, IDatabase database, INotification notification)
        {
            _twitter = twitter;
            _mastodon = mastodon;
            _database = database;
            _notification = notification;
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
                        if (displayAttribute == null)
                        {
                            continue;
                        }

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

        private async Task<UserData> GetUserByMastodonHandle(string mastodonHandle)
        {
            // TODO: Error handling
            var parts = mastodonHandle.Split('@');
            return await _database.GetUserByUsername(parts[0], parts[1]);
        }

        public async Task<List<Status>> GetTootsContainingAsync(string mastodonHandle, string searchstring, int limit)
        {
            var user = await GetUserByMastodonHandle(mastodonHandle);
            var toots = await _mastodon.GetTootsContainingAsync(user, searchstring, limit);
            return toots;
        }

        private long? GetTwitterReplyToFromTootIdForUser(UserData user, string tootId)
        {
            if (user.Crossposts.Any(q => q.TootId == tootId))
            {
                var twitterIds = user.Crossposts.FirstOrDefault(q => q.TootId == tootId)?.TwitterIds;
                if (twitterIds != null && twitterIds.Count > 0) return twitterIds.Max();
            }
            return null;
        }

        private async Task<long?> GetTwitterReplyToIdFromTootId(UserData currentUser, string tootId)
        {
            long? replyToId = GetTwitterReplyToFromTootIdForUser(currentUser, tootId);
            if (replyToId != null) return replyToId;

            var allUsers = await _database.GetAllUsers();
            foreach (var user in allUsers)
            {
                replyToId = GetTwitterReplyToFromTootIdForUser(user, tootId);
                if (replyToId != null) return replyToId;
            }
            return null;
        }

        private async Task<long?> GetTwitterReplyId(UserData currentUser, Status toot)
        {
            if (toot.InReplyToId == null) return null;
            return await GetTwitterReplyToIdFromTootId(currentUser, toot.InReplyToId);
        }

        private async Task<List<Crosspost>> SendTootsAsync(UserData user, List<Status> toots)
        {
            var sentToots = new List<Crosspost>();
            _logger.LogTrace("sending {tootcount} toots for {completename}", toots.Count, user.Mastodon.CompleteName);
            var newLastDate = DateTime.UtcNow;
            foreach (var toot in toots)
            {
                var timeToTweet = toot.CreatedAt.Add(user.Config.Delay);
                if (timeToTweet > DateTime.UtcNow)
                {
                    _logger.LogTrace("Won't tweet until {startdate} (utc)", timeToTweet);
                    continue;
                }

                user.Update = true;
                try
                {
                    var twitterReplyToId = await GetTwitterReplyId(user, toot);
                    _logger.LogTrace("Toot: {id}|{url}", toot.Id, toot.Uri);
                    if (user.Crossposts.FirstOrDefault(q => q.TootId == toot.Id) != null)
                    {
                        sentToots.Add(new Crosspost { Result = "AlreadyTweeted", TootId = toot.Id });
                        _logger.LogWarning("already tweeted this");
                        continue;
                    }

                    Crosspost crosspost;
                    if (toot.Id == user.Mastodon.LastToot)
                    {
                        crosspost = new Crosspost { Result = "AlreadyTweeted", TootId = toot.Id };
                        _logger.LogWarning("already tweeted");
                    }
                    else if (toot.InReplyToId != null && twitterReplyToId == null)
                    {
                        crosspost = new Crosspost { Result = "IsReply", TootId = toot.Id };
                        _logger.LogDebug("is a reply to an unknwon toot. Wont tweet");
                    }
                    else
                    {
                        var twitterIds = await _twitter.PublishAsync(user, toot, twitterReplyToId);
                        crosspost = new Crosspost { Result = "Tweeted", TootId = toot.Id, TwitterIds = twitterIds };
                    }
                    user.Crossposts.Add(crosspost);
                    sentToots.Add(crosspost);
                }
                catch (TwitterException twitterException)
                {
                    var firstTwitterException = twitterException.TwitterExceptionInfos.FirstOrDefault();
                    if (firstTwitterException == null)
                    {
                        _logger.LogError(twitterException, "Unknown Twitter Exception when trying to tweet toot nr {id} from {user}. Will not retry\n", toot.Id, user.Id);
                        sentToots.Add(new Crosspost { Result = "TwitterException", TootId = toot.Id });
                    }
                    else
                    {
                        switch (firstTwitterException.Code)
                        {
                            case 88:
                                _notification.Error(user, MessageCodes.RateLimit);
                                sentToots.Add(new Crosspost { Result = "RateLimit", TootId = toot.Id });
                                _logger.LogCritical("Rate Limit reached");
                                break;

                            case 89:
                                _notification.Error(user, MessageCodes.TwitterAuthError);
                                user.BlockDate = DateTime.UtcNow;
                                user.BlockReason = UserData.BlockReasons.AuthTwitter;
                                sentToots.Add(new Crosspost { Result = "TwitterAuth", TootId = toot.Id });
                                _logger.LogWarning("User {id} has been blocked because twitter auth was revoked", user.Id);
                                break;

                            default:
                                _logger.LogError("Unknown Twitter Errorcode {code}: {message}", firstTwitterException.Code, firstTwitterException.Message);
                                sentToots.Add(new Crosspost { Result = "TwitterExceptionDef", TootId = toot.Id });
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Publishing tweet failed. tootid: {tootid} user:{userid} Will NOT retry\n", toot.Id, user.Mastodon.CompleteName);
                    sentToots.Add(new Crosspost { Result = "Exception", TootId = toot.Id });
                }
                user.Mastodon.LastToot = toot.Id;
                user.Mastodon.LastTootDate = newLastDate;
            }

            var sentCount = sentToots.Count(q => q.TwitterIds.Count > 0);
            if (sentCount > 0)
            {
                _logger.LogDebug("Sent {sentCount} from {tootsCount} toots to twitter for {completename}", sentCount, toots.Count, user.Mastodon.CompleteName);
            }

            return sentToots;
        }

        public async Task InviteAsync(string mastodonHandle)
        {
            await _mastodon.SendStatusMessageToAsync(null, $"{mastodonHandle} [INVITE] ", MessageCodes.Invite, null);
        }

        public async Task SendSingleTootAsync(UserData user, string tootId)
        {
            try
            {
                var toot = await _mastodon.GetSingleTootAsync(user, tootId);
                if (toot == null)
                {
                    _logger.LogError("toot {tootid} not found for user {userid}", tootId, user.Id);
                    return;
                }

                await SendTootsAsync(user, new List<Status> { toot });
                _logger.LogDebug("sent single toot {tootid}", tootId);
            }
            catch (Mastonet.ServerErrorException mastodonException)
            {
                _notification.Error(user, MessageCodes.MastodonAuthError, $"Error Message: {mastodonException.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending single toot");
            }
        }

        public async Task SendTootsForAllUsersAsync()
        {
            var users = (await _database.GetActiveUsers()).ToList();
            GlobalStorage.FillGlobalReplacements(users);

            var toots = new List<Crosspost>();

            foreach (var user in users)
            {
                bool blockUser = false;
                List<Status> userToots;
                try
                {
                    userToots = await _mastodon.GetNonPostedTootsAsync(user);
                }
                catch (Mastonet.ServerErrorException mastodonException)
                {
                    _notification.Error(user, MessageCodes.MastodonAuthError, $"Error Message: {mastodonException.Message}");
                    blockUser = true;
                    userToots = new List<Status>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending single toot. user: {username}. \n", user.Mastodon.CompleteName);
                    userToots = new List<Status>();
                }
                if (blockUser)
                {
                    user.BlockDate = DateTime.UtcNow;
                    user.BlockReason = UserData.BlockReasons.AuthMastodon;
                    user.Update = true;
                }

                if (userToots?.Count > 0) toots.AddRange(await SendTootsAsync(user, userToots));

                if (user.Update) await _database.UpsertUser(user);
            }
            var tootCount = toots.Count(q => q.TwitterIds.Count > 0);
            if (tootCount > 0)
                _logger.LogInformation("Sent {tootCount} toots for {count} users", tootCount, users.Count());
        }

        public async Task<TootConfigurationAppModes.ValidModes> GetServerMode()
        {
            var serverMode = _config.App.Modes.Active;
            var serverStats = await _database.GetServerStats();

            if (_config.App.Modes.AutoInvite > 0 && serverMode == TootConfigurationAppModes.ValidModes.Closed && _config.App.Modes.AutoInvite <= serverStats.ActiveUsers) serverMode = TootConfigurationAppModes.ValidModes.Invite;
            if (_config.App.Modes.AutoClosed > 0 && _config.App.Modes.AutoClosed <= serverStats.ActiveUsers) serverMode = TootConfigurationAppModes.ValidModes.Closed;

            return serverMode;
        }

        public async Task CalculateServerStats()
        {
            var serverstats = await _database.GetServerStats();
            var allUsers = await _database.GetActiveUsers();
            var activeUsers = allUsers.Where(q => q.Crossposts.Any(q => q.CreatedAt >= DateTime.UtcNow.AddDays(-1)));
            serverstats.ActiveUsers = activeUsers.Count();
            serverstats.TotalUsers = allUsers.Count();
            await _database.UpSertServerStats(serverstats);
            _logger.LogDebug("Updated Serverstats.  {serverstats} ", serverstats);
        }
    }
}
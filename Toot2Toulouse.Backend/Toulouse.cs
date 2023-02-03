using LiteDB;

using Mastonet.Entities;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;

using System.Reflection;
using System.Runtime.Intrinsics.X86;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi.Exceptions;

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

        private UserData GetUserByMastodonHandle(string mastodonHandle)
        {
            // TODO: Error handling
            var parts = mastodonHandle.Split('@');
            return _database.GetUserByUsername(parts[0], parts[1]);
        }

        public async Task<List<Status>> GetTootsContainingAsync(string mastodonHandle, string searchstring, int limit)
        {
            var user = GetUserByMastodonHandle(mastodonHandle);
            var toots = await _mastodon.GetTootsContainingAsync(user.Id, searchstring, limit);
            return toots;
        }

        private async Task<List<Crosspost>> SendTootsAsync(Guid userId, List<Status> toots, bool updateUserData)
        {
            var sentToots = new List<Crosspost>();
            var user = _database.GetUserById(userId);
            _logger.LogDebug("sending {tootcount} toots for {user}", toots.Count, user.Mastodon.DisplayName);
            var newLastDate = DateTime.UtcNow;
            foreach (var toot in toots)
            {
                // TODO: DELAY

                try
                {
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
                    else if (toot.InReplyToId != null)
                    {
                        crosspost = new Crosspost { Result = "IsReply", TootId = toot.Id };
                        _logger.LogDebug("is a reply. Wont tweet");
                    }
                    else
                    {
                        var twitterIds = await _twitter.PublishAsync(user, toot);
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
                        _logger.LogError(twitterException, "Unknown Twitter Exception when trying to tweet toot nr {id} from {user}. Will not retry", toot.Id, userId);
                        sentToots.Add(new Crosspost { Result = "TwitterException", TootId = toot.Id });
                    }
                    else
                    {
                        switch (firstTwitterException.Code)
                        {
                            case 88:
                                _notification.Error(Guid.Empty, TootConfigurationApp.MessageCodes.RateLimit);
                                sentToots.Add(new Crosspost { Result = "RateLimit", TootId = toot.Id });
                                _logger.LogCritical("Rate Limit reached");
                                break;

                            case 89:
                                _notification.Error(userId, TootConfigurationApp.MessageCodes.TwitterAuthError);
                                user.BlockDate = DateTime.Now;
                                user.BlockReason = UserData.BlockReasons.AuthTwitter;
                                sentToots.Add(new Crosspost { Result = "TwitterAuth", TootId = toot.Id });
                                _logger.LogWarning("User {id} has been blocked because twitter auth was revoked", userId);
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
                    _logger.LogWarning(ex, "Publishing tweet failed. Will NOT retry");
                    sentToots.Add(new Crosspost { Result = "Exception", TootId = toot.Id });
                }
                user.Mastodon.LastToot = toot.Id;
            }
            user.Mastodon.LastTootDate = newLastDate;
            if (updateUserData) _database.UpsertUser(user);
            var sentCount=sentToots.Count(q=>q.TwitterIds.Count > 0);

            _logger.LogDebug($"Sent {sentCount} from {toots.Count} toots to twitter for {user.Mastodon.Handle}@{user.Mastodon.Instance}");

           
            return sentToots;
        }

        public async Task InviteAsync(string mastodonHandle)
        {
            await _mastodon.SendStatusMessageToAsync(Guid.Empty, $"{mastodonHandle} [INVITE] ", TootConfigurationApp.MessageCodes.Invite, null);
        }

        public async Task SendSingleTootAsync(Guid userId, string tootId)
        {
            bool blockUser = false;
            try
            {
                var toot = await _mastodon.GetSingleTootAsync(userId, tootId);
                if (toot == null)
                {
                    _logger.LogError("toot {tootid} not found for user {userid}", tootId, userId);
                    return;
                }

                await SendTootsAsync(userId, new List<Status> { toot }, false);
                _logger.LogDebug("sent single toot {tootid}", tootId);
            }
            catch (Mastonet.ServerErrorException mastodonException)
            {
                _notification.Error(userId, TootConfigurationApp.MessageCodes.MastodonAuthError, $"Error Message: {mastodonException.Message}");
                blockUser = true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed sending single toot");
            }
            if (blockUser)
            {
            }
        }

        public async Task SendTootsForAllUsersAsync()
        {
            var users = _database.GetActiveUsers().ToList();
            List<Crosspost> toots = new List<Crosspost>();

            foreach (var user in users)
            {
                bool blockUser = false;
                List<Status> userToots; //=null; //=new List<Status>();
                try
                {
                    userToots = await _mastodon.GetNonPostedTootsAsync(user.Id);
                }
                catch (Mastonet.ServerErrorException mastodonException)
                {
                    _notification.Error(user.Id, TootConfigurationApp.MessageCodes.MastodonAuthError, $"Error Message: {mastodonException.Message}");
                    blockUser = true;

                    userToots = new List<Status>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending single toot");
                    userToots = new List<Status>();
                }
                if (blockUser)
                {
                    user.BlockDate = DateTime.Now;
                    user.BlockReason = UserData.BlockReasons.AuthMastodon;
                    _database.UpsertUser(user);
                }

                if (userToots?.Count > 0) toots.AddRange(await SendTootsAsync(user.Id, userToots, true));
            }
            var tootCount = toots.Count(q => q.TwitterIds.Count > 0);
            var logEntry = $"Sent {tootCount} toots for {users.Count()} users";
            if (tootCount==0)
            {
                _logger.LogTrace(logEntry);
            } else
            {
                _logger.LogInformation(logEntry);
            }
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
            var allUsers = _database.GetActiveUsers();
            var activeUsers = allUsers.Where(q => q.Crossposts.Any(q => q.CreatedAt >= DateTime.Now.AddDays(-1)));
            serverstats.ActiveUsers = activeUsers.Count();
            serverstats.TotalUsers = allUsers.Count();
            _database.UpSertServerStats(serverstats);
            _logger.LogDebug("Updated Serverstats.  {serverstats} ", serverstats);
        }
    }
}
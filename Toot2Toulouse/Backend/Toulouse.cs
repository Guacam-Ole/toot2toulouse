using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi;
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

            // Quick and dirty until storage methods implemented:
            var userCredentials = configReader.ReadJsonFile<TwitterCredentials>("developmentUserCredentials.json");
            var userClient = new TwitterClient(userCredentials);
            _twitter.InitUserAsync(userClient, _config.Defaults);
        }

        public async Task TweetServicePostsAsync()
        {
            await TweetServicePostsContaining("[VIDEO]", "[YT]");
            //await TweetServicePostsContaining( "[MULTI]");
        }

        public async Task TweetServicePostsContaining(params string[] content)
        {
            foreach (var item in content)
            {
                await TweetServicePostContaining(item);
            }
        }

        public async Task TweetServicePostContaining(string content)
        {
            var toots = await _mastodon.GetServicePostsContainingAsync(content);
            if (toots != null && toots.Count() > 0)
            {
                foreach (var toot in toots) await _twitter.PublishAsync(toot);
            }
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

        public string GetHashString(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

            var sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        public void AddHashToUser(User user)
        {
            user.Hash = CalculateHashForUser(user);
        }

        public User? GetUserByHash(Guid userId, string hash)
        {
            return _database.GetUserByIdAndHash(userId, hash);
        }

        private string CalculateHashForUser(User user)
        {
            string valueToHash = $"{user.Id}{user.Mastodon.Id}{user.Twitter.Id}{_config.Secrets.Salt}";
            return GetHashString(valueToHash);
        }
    }
}
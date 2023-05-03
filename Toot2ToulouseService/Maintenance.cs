using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

namespace Toot2ToulouseService
{
    public class Maintenance
    {
        private readonly ILogger<Maintenance> _logger;
        private readonly IDatabase _database;
        private readonly IToulouse _toulouse;
        private readonly IUser _user;
        private readonly TootConfiguration _config;

        public Maintenance(ILogger<Maintenance> logger, ConfigReader configReader, IDatabase database, IToulouse toulouse, IUser user)
        {
            _logger = logger;
            _database = database;
            _toulouse = toulouse;
            _user = user;
            _config = configReader.Configuration;
        }

        private async void GetUpgradePath(Version databaseVersion, Version currentVersion)
        {
            int databaseVersionInt = databaseVersion.Major * 100 + databaseVersion.Minor;
            int currentVersionInt = currentVersion.Major * 100 + currentVersion.Minor;

            if (currentVersionInt <= databaseVersionInt) return;
            for (int i = databaseVersionInt + 1; i <= currentVersionInt; i++)
            {
                await DoUpgradeFor($"{i / 100}.{i % 100}");
            }
        }

        private async Task DoUpgradeFor(string version)
        {
            switch (version)
            {
                case "0.9":
                    var allUsers = await _database.GetActiveUsers();
                    foreach (var user in allUsers)
                    {
                        user.Config.VisibilitiesToPost = _config.Defaults.VisibilitiesToPost;
                        await _database.UpsertUser(user);
                    }
                    break;
            }
        }

        public async Task InviteAsync(string mastodonHandle)
        {
            await _toulouse.InviteAsync(mastodonHandle);
        }

        public async Task Upgrade(Version? fromVersion)
        {
            var serverstats = await _database.GetServerStats();
            fromVersion ??= new Version(serverstats.CurrentVersion ?? "0.0");
            if (fromVersion.ToString() == serverstats.CurrentVersion) return;
            GetUpgradePath(fromVersion, _config.CurrentVersion);
            serverstats.CurrentVersion = _config.CurrentVersion.ToString();
            await _database.UpSertServerStats(serverstats);
        }

        public string GetVersion()
        {
            return _config.CurrentVersion.ToString();
        }

        private string MinLength(string value, int minLength)
        {
            while (value.Length < minLength) value += " ";
            return value;
        }

        private string WriteLine(string seperator = "\t", params object[] elements)
        {
            var line = string.Empty;
            bool isContent = true;
            object currentElement = string.Empty;

            foreach ( var element in elements)
            {
                if (isContent)
                {
                    currentElement = element;

                }else
                {
                    if (currentElement==null)
                    {
                        line += MinLength(string.Empty, (int)element);
                    } else
                    {
                        line += MinLength(currentElement.ToString(), (int)element);
                    }
                    line += seperator;
                }
                isContent = !isContent;
            }

            line = line[..^seperator.Length];
                
         //       line[^seperator.Length..]+"\n";
            return line;
        }

        public async Task ListIds()
        {
            await Console.Out.WriteLineAsync(WriteLine("\t", "id",32,"mastodon",30,"twitter",30,"last",20,"count",5, "blockreason",12,"blockdate",20));
            var allUsers = await _database.GetAllUsers();
            foreach (var user in allUsers)
            {
                await Console.Out.WriteLineAsync(WriteLine("\t",
                    user.Id, 32,
                    user.Mastodon.CompleteName, 30,
                    user.Twitter.Handle, 30,
                    user.Crossposts?.Max(q => q.CreatedAt) , 20,
                    user.Crossposts?.Count, 5,
                    user.BlockReason, 12,
                    user.BlockDate, 20));
            }
            _logger.LogInformation("Retrieved all userIds");
        }

        public async Task BlockUser(Guid userId)
        {
            await _user.Block(userId, Toot2Toulouse.Backend.Models.UserData.BlockReasons.Manual);
            Console.WriteLine("User blocked");
        }

        public async Task UnblockUser(Guid userId)
        {
            await _user.Unblock(userId);
            Console.WriteLine("User unblocked");
        }

        public async Task CleanUp()
        {
            var killDate = DateTime.Now.AddDays(-_config.App.Intervals.AuthFailureDeleteDays);
            var allUsers = await _database.GetAllUsers();
            foreach (var user in allUsers)
            {
                if (user.BlockDate == null || user.BlockReason == null || user.BlockDate > killDate) continue;
                switch (user.BlockReason.Value)
                {
                    case Toot2Toulouse.Backend.Models.UserData.BlockReasons.Manual:
                        _logger.LogDebug("Won't remove {user}  {mastodon} because is blocked manually. Will remove all other data, though", user.Id, user.Mastodon?.CompleteName);
                        user.Crossposts = new List<Toot2Toulouse.Backend.Models.Crosspost>();
                        user.Mastodon.Secret = null;
                        user.Twitter.AccessSecret = null;
                        user.Twitter.AccessToken = null;
                        user.Twitter.Bearer = null;
                        user.Twitter.ConsumerKey = null;
                        user.Twitter.ConsumerSecret = null;
                        user.Config = new UserConfiguration();
                        await _database.UpsertUser(user);
                        break;

                    case Toot2Toulouse.Backend.Models.UserData.BlockReasons.AuthMastodon:
                    case Toot2Toulouse.Backend.Models.UserData.BlockReasons.AuthTwitter:
                        _logger.LogInformation("Remove {user} ({mastodon}-{twitter})", user.Id, user.Mastodon?.CompleteName, user.Twitter?.Handle);
                        await _database.RemoveUser(user.Id);
                        break;

                    default:
                        _logger.LogWarning("Unexpected blockreason '{reason}'. Cannot remove user", user.BlockReason);
                        break;
                }
            }
            _logger.LogInformation("Cleanup finished");
        }

        public async Task CollectStats()
        {
            await _toulouse.CalculateServerStats();
            _logger.LogInformation("collected and stored stats");
        }


        public async Task Ping()
        {
            if (_config.App.Stats.Ping == null || _config.App.Stats.Ping.Count == 0)
            {
                _logger.LogInformation("Nothing to ping");
                return;
            }

            var stats = await _toulouse.CalculateServerStats();
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var url in _config.App.Stats.Ping)
            {
                var pingdata = new PingData { Stats = stats, Config = _config.App };
                var response = await client.PostAsJsonAsync(url, JsonConvert.SerializeObject(pingdata));
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Ping to {url} returned {statuscode}", url, response.StatusCode);
            }
        }
    }
}
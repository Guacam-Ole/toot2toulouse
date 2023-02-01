using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2ToulouseService
{
    public class Maintenance
    {
        private readonly ILogger<Maintenance> _logger;
        private readonly IDatabase _database;
        private readonly IToulouse _toulouse;
        private readonly TootConfiguration _config;

        public Maintenance(ILogger<Maintenance> logger, ConfigReader configReader, IDatabase database, IToulouse toulouse)
        {
            _logger = logger;
            _database = database;
            _toulouse = toulouse;
            _config = configReader.Configuration;
        }

        private void GetUpgradePath(Version databaseVersion, Version currentVersion)
        {
            int databaseVersionInt = databaseVersion.Major * 100 + databaseVersion.Minor;
            int currentVersionInt = currentVersion.Major * 100 + currentVersion.Minor;

            if (currentVersionInt <= databaseVersionInt) return;
            for (int i = databaseVersionInt + 1; i <= currentVersionInt; i++)
            {
                DoUpgradeFor($"{i / 100}.{i % 100}");
            }
        }

        private void DoUpgradeFor(string version)
        {
            switch (version)
            {
                case "0.9":
                    var allUsers = _database.GetAllValidUsers();
                    foreach (var user in allUsers)
                    {
                        user.Config.VisibilitiesToPost = _config.Defaults.VisibilitiesToPost;
                        _database.UpsertUser(user);
                    }
                    break;
            }
        }

        public async Task Invite(string mastodonHandle)
        {
            await _toulouse.Invite(mastodonHandle);
        }

        public void Upgrade(Version? fromVersion)
        {
            var serverstats = _database.GetServerStats();
            fromVersion ??= new Version(serverstats.CurrentVersion ?? "0.0");
            if (fromVersion.ToString() == serverstats.CurrentVersion) return;
            GetUpgradePath(fromVersion, _config.CurrentVersion);
            serverstats.CurrentVersion = _config.CurrentVersion.ToString();
            _database.UpSertServerStats(serverstats);
        }

        public string GetVersion()
        {
            return _database.GetServerStats().CurrentVersion;
        }

        public void ListIds()
        {
            _database.GetAllValidUsers().ForEach(user =>
            {
                Console.WriteLine($"{user.Id}->{user.Mastodon?.Handle}@{user.Mastodon?.Instance}->{user.Twitter?.Handle}");

            });
        }
    }
}
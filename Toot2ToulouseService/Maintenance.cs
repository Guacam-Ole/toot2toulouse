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

        private async Task DoUpgradeFor(string version)
        {
            switch (version)
            {
                case "0.9":
                    var allUsers =  await _database.GetActiveUsers();
                    foreach (var user in allUsers)
                    {
                        user.Config.VisibilitiesToPost = _config.Defaults.VisibilitiesToPost;
                        _database.UpsertUser(user);
                    }
                    break;
            }
        }

        public async Task InviteAsync(string mastodonHandle)
        {
            await _toulouse.InviteAsync(mastodonHandle);
        }

        public async Task    Upgrade(Version? fromVersion)
        {
            var serverstats = await _database.GetServerStats();
            fromVersion ??= new Version(serverstats.CurrentVersion ?? "0.0");
            if (fromVersion.ToString() == serverstats.CurrentVersion) return;
            GetUpgradePath(fromVersion, _config.CurrentVersion);
            serverstats.CurrentVersion = _config.CurrentVersion.ToString();
            _database.UpSertServerStats(serverstats);
        }

        public string GetVersion()
        {
            return _config.CurrentVersion.ToString();
        }

        public async void ListIds()
        {
            Console.WriteLine("id\tblockreason\tblockdate\tmastodon\ttwitter");
            var allUsers=await _database.GetAllUsers();
            allUsers.ForEach(user =>
            {
                Console.WriteLine($"{user.Id}\t{user.BlockReason}\t{user.BlockDate}\t{user.Mastodon?.Handle}@{user.Mastodon?.Instance}\t{user.Twitter?.Handle}");
            });
            _logger.LogInformation("Retrieved all userIds");
        }

        public void BlockUser(Guid userId)
        {
            _user.Block(userId, Toot2Toulouse.Backend.Models.UserData.BlockReasons.Manual);
            Console.WriteLine("User blocked");
        }

        public void UnblockUser(Guid userId)
        {
            _user.Unblock(userId);
            Console.WriteLine("User unblocked");
        }
    }
}
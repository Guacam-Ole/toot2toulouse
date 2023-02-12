using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2ToulouseService
{
    public class Publish
    {
        private readonly ILogger<Publish> _logger;
        private readonly IToulouse _toulouse;
        private readonly IDatabase _database;
        private readonly TootConfiguration _config;

        public Publish(ILogger<Publish> logger, IToulouse toulouse, ConfigReader configReader, IDatabase database)
        {
            _logger = logger;
            _toulouse = toulouse;
            _database = database;
            _config = configReader.Configuration;
        }

        public async Task PublishTootsAsync(bool loop)
        {
            do
            {
                await _toulouse.SendTootsForAllUsersAsync();

                if (loop) Thread.Sleep((int)_config.App.Intervals.Sending.TotalMilliseconds);
            } while (loop);
        }

        public async Task PublishSingleTootAsync(Guid userId, string tootId)
        {
            var user=await _database.GetUserById(userId);
            await _toulouse.SendSingleTootAsync(user, tootId);
        }

        public async Task<List<Status>> GetTootsContainingAsync(string mastodonHandle, string contents, int searchLimit)
        {
            return await _toulouse.GetTootsContainingAsync(mastodonHandle, contents, searchLimit);
        }
    }
}
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
        private readonly TootConfiguration _config;

        public Publish(ILogger<Publish> logger, IToulouse toulouse, ConfigReader configReader)
        {
            _logger = logger;
            _toulouse = toulouse;
            _config = configReader.Configuration;
        }

        public async Task PublishToots(bool loop)
        {
            do
            {
                await _toulouse.SendTootsForAllUsers();
                if (loop) Thread.Sleep((int)_config.App.Intervals.Sending.TotalMilliseconds);
            } while (loop);
        }

        public async Task PublishTootsContaining(string mastodonHandle, string contents, int searchLimit)
        {
            await _toulouse.GetTootsContaining(mastodonHandle, contents, searchLimit);
        }
    }
}
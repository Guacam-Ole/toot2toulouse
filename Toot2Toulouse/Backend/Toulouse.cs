using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

using Tweetinvi;
using Tweetinvi.Models;

namespace Toot2Toulouse.Backend
{
    public class Toulouse : IToulouse
    {
        private readonly ITwitter _twitter;
        private readonly IMastodon _mastodon;
        private readonly TootConfiguration _config;
        private readonly ILogger<Toulouse> _logger;

        public Toulouse(ILogger<Toulouse> logger, ConfigReader configReader, ITwitter twitter, IMastodon mastodon  )
        {
            _twitter = twitter;
            _mastodon = mastodon;
            _config = configReader.Configuration;
            _logger = logger;

            // Quick and dirty until storage methods implemented:
            var userCredentials = configReader.ReadJsonFile<TwitterCredentials>("developmentUserCredentials.json");
            var userClient = new TwitterClient(userCredentials);
            _twitter.InitUserAsync(userClient, _config.Defaults);
        }

        public async Task TweetServicePostsAsync()
        {
            //await TweetServicePostsContaining("[ONLYMENTIONED]", "[EMOJI]", "[MULTI]"); 
            await TweetServicePostsContaining("[YT]");
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
            if (toots != null)
            {
                foreach (var toot in toots) await _twitter.PublishAsync(toot);
            }
        }
    }
}
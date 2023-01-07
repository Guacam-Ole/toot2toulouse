using Newtonsoft.Json;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Controllers;

using Tweetinvi;
using Tweetinvi.Models;

namespace Toot2Toulouse.Backend
{
    public class App
    {
        private readonly ITwitter _twitter;
        private readonly Mastodon _mastodon;
        private readonly TootConfiguration _config;
        private readonly ILogger<App> _logger;

        public App(ITwitter twitter, Mastodon mastodon, ConfigReader configReader, ILogger<App> logger)
        {
            _twitter = twitter;
            _mastodon = mastodon;
            _config = configReader.Configuration;
            _logger = logger;

            // Quick and dirty until storage methods implemented:
            var userCredentials = configReader.ReadJsonFile<TwitterCredentials>("developmentUserCredentials.json");
            var userClient = new TwitterClient(userCredentials);
            _twitter.InitUser(userClient, _config.Defaults);
        }


        public async Task TweetServicePosts()
        {


            await TweetServicePostsContaining("[ONLYMENTIONED]");
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
            var toot = await _mastodon.GetPostContaining(content);
            if (toot != null)
            {
                await _twitter.Publish(toot);
            }
        }
    }
}

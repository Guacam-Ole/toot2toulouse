using Newtonsoft.Json;

using System.Text.RegularExpressions;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Models;
using Tweetinvi.Parameters;


namespace Toot2Toulouse.Backend
{
    public class Twitter : ITwitter
    {
        private TootConfiguration _config;
        private TwitterClient _appClient;
        private TwitterClient _userClient;
        private UserConfiguration _userConfiguration;

        private static readonly IAuthenticationRequestStore _twitterRequestStore = new LocalAuthenticationRequestStore();
        private readonly ILogger<Twitter> _logger;
        private readonly IToot _toot;

        public Twitter(ILogger<Twitter> logger, ConfigReader configReader, IToot toot)
        {
            _config = configReader.Configuration;
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
            _logger = logger;
            _toot = toot;
        }

        public void InitUser(TwitterClient client, UserConfiguration userConfiguration)
        {
            _userClient = client;
            _userConfiguration = userConfiguration;
            _toot.InitUser(userConfiguration);
        }

        public async Task<string> GetAuthenticationUrlAsync(string baseUrl)
        {
            try
            {
                var guid = Guid.NewGuid();
                var targetUrl = baseUrl + "/TwitterAuth";
                var redirectUrl = _twitterRequestStore.AppendAuthenticationRequestIdToCallbackUrl(targetUrl, guid.ToString());
                var authTokenRequest = await _appClient.Auth.RequestAuthenticationUrlAsync(redirectUrl);
                await _twitterRequestStore.AddAuthenticationTokenAsync(guid.ToString(), authTokenRequest);
                return authTokenRequest.AuthorizationURL;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed retrieving AuthenticationUrl", ex);
                throw;
            }
        }

        private void TestAuthenticate()
        {
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret, _config.Secrets.Twitter.Personal.AccessToken, _config.Secrets.Twitter.Personal.AccessTokenSecret);
        }

       

  




        //public async Task AuthTest()
        //{
        //    TestAuthenticate();
        //    var user = await _appClient.Users.GetAuthenticatedUserAsync();
        //    Console.WriteLine($"Moinsen, {user}");

        //    string suffix = " [🐘²🐦]";
        //    await Tweet("Nur ein simpler Testtweet via API. Einfach ignorieren.  " + suffix);
        //}

        private string StripHtml(string content)
        {
            content= content.Replace("</p>", "\n\n");
            content = content.Replace("<br />", "\n");
            return Regex.Replace(content, "<[a-zA-Z/].*?>", String.Empty);
        }

        private bool ShouldITweetThis(Mastonet.Entities.Status toot)
        {
            // TODO: CHeck visibilitysettings
            return true;
        }

        public async Task PublishAsync(Mastonet.Entities.Status toot)
        {
            if (!ShouldITweetThis(toot)) return;
            string tootContent = StripHtml(toot.Content);
            string contentWarning = StripHtml(toot.SpoilerText);
            bool hasContentWarning = !string.IsNullOrWhiteSpace(contentWarning);
            if (toot.MediaAttachments!=null && toot.MediaAttachments.Count()>0)
            {
                tootContent += "\n";
                foreach (var attachment in toot.MediaAttachments)
                {
                 //   tootContent += attachment.Url + "\n";
                }
            }
          // await Tweet(tootContent);

        }

        public async Task PublishFromToot(string content)
        {
            var replies = _toot.GetReplies(content, out string mainTweet);
            if (replies != null)
            {
                switch (_userConfiguration.LongContent)
                {
                    case ITwitter.LongContent.DontPublish:
                        break;

                    case ITwitter.LongContent.Cut:
                        await TweetAsync( mainTweet);
                        break;

                    case ITwitter.LongContent.Thread:
                        var tweet = await TweetAsync( mainTweet);

                        if (tweet.Id != 0)
                        {
                            foreach (var reply in replies)
                            {
                                tweet = await TweetAsync( reply, tweet.Id);
                            }
                        }
                        break;
                }
            }
            else
            {
                await TweetAsync(mainTweet);
            }
        }

        public async Task<ITweet> TweetAsync(string content, long? replyTo = null)
        {
            try
            {
                return await _userClient.Tweets.PublishTweetAsync(new PublishTweetParameters
                {
                    Text = content,
                    InReplyToTweetId = replyTo                });
            }
            catch (Exception ex)
            {
                // TODO: LOG
                return null;
            }
        }

        public async Task<bool> FinishAuthenticationAsync(string query)
        {
            var requestParameters = await RequestCredentialsParameters.FromCallbackUrlAsync(query, _twitterRequestStore);
            var userCredentials = await _appClient.Auth.RequestCredentialsAsync(requestParameters);

            var credsStr = JsonConvert.SerializeObject(userCredentials);
            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();

            string suffix = " [🐘²🐦]";
            //    await Tweet(userClient, "Nur ein simpler Testtweet via API. Einfach ignorieren :)   " + suffix);

            string longText = "Das ist ein langer Thread. Bitte ignorieren..." +
                @"Rope's end gangplank hang the jib squiffy warp doubloon bilge rat hulk reef scuttle. Haul wind belay Sea Legs tender maroon rigging skysail jack knave holystone. Ho lugger transom Yellow Jack gaff Jolly Roger fire in the hole topmast ballast Barbary Coast.

Jack Tar killick fathom Admiral of the Black quarter hearties hempen halter ahoy careen mizzen. Pinnace hang the jib aft grog blossom plunder ye log reef sails lass rutters. Aft bucko lad rutters scallywag trysail handsomely galleon lass Buccaneer.

Yawl lateen sail carouser smartly fire ship pirate Nelsons folly league code of conduct Sea Legs. Scuppers driver loaded to the gunwalls Arr gangplank Sink me poop deck pillage lugger snow. Hempen halter bounty crimp come about grog blossom pink barque galleon wherry cable. ";

            InitUser(userClient, _config.Defaults);
            await PublishFromToot(longText);
            return true;
        }
    }
}
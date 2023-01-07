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

        public enum Visibilities
        {
            PublicAll,
            PublicFollowers,
            PublicMentionedHide,
            PublicMentionedShow,
            Circle,
            DontPublish
        }

        public enum ContentWarnings
        {
            DontPublish,
            NoCw,
            NoCwSensitive,
            WithCw,
            WithCwSensitive,
        }

        public enum Replies
        {
            Publish,
            DontPublish,
            Thread
        }

        public enum LongContent
        {
            Cut,
            DontPublish,
            Thread
        }

        public enum Followersearch
        {
            TwitterContactName,
            TwitterContactDescription,
            PersonalTranslation,
            GlobalTranslation,
            Text,
            Backlink
        }

        public Twitter(ConfigReader configReader)
        {
            _config = configReader.Configuration;
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
        }

        public void InitUser(TwitterClient client, UserConfiguration userConfiguration)
        {
            _userClient = client;
            _userConfiguration = userConfiguration;
        }

        public async Task<string> GetAuthenticationUrl(string baseUrl)
        {
            var guid = Guid.NewGuid();
            var targetUrl = baseUrl + "/TwitterAuth";
            var redirectUrl = _twitterRequestStore.AppendAuthenticationRequestIdToCallbackUrl(targetUrl, guid.ToString());
            var authTokenRequest = await _appClient.Auth.RequestAuthenticationUrlAsync(redirectUrl);
            await _twitterRequestStore.AddAuthenticationTokenAsync(guid.ToString(), authTokenRequest);
            return authTokenRequest.AuthorizationURL;
        }

        private void TestAuthenticate()
        {
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret, _config.Secrets.Twitter.Personal.AccessToken, _config.Secrets.Twitter.Personal.AccessTokenSecret);
        }

        private List<string>? GetReplies(string originalToot, out string mainTweet)
        {
            DoReplacements(ref originalToot);
            int maxLength = _config.App.TwitterCharacterLimit;
            bool addSuffix = true;

            string suffix = _userConfiguration.AppSuffix.Content ?? string.Empty;

            bool needsSplit = originalToot.Length > maxLength;
            if (!needsSplit && originalToot.Length + suffix.Length > maxLength && _userConfiguration.AppSuffix.HideOnLongText) addSuffix = false;
            mainTweet = originalToot;
            if (addSuffix) mainTweet += suffix;

            if (!needsSplit) return null;

            var replylist = new List<string>();
            mainTweet = GetChunk(mainTweet, maxLength, true, out string? replies);
            while (replies != null)
            {
                replylist.Add(GetChunk(replies, maxLength, false,  out replies));
            }

            return replylist;
        }

        private void DoReplacements(ref string texttopublish)
        {
            texttopublish = $" {texttopublish} ";
            foreach (var translation in _userConfiguration.Replacements)
            {
                texttopublish = texttopublish.Replace($" {translation.Key} ", $" {translation.Value} ", StringComparison.CurrentCultureIgnoreCase);
            }
            texttopublish = texttopublish.Trim();
        }

        private string GetChunk(string completeText, int maxLength, bool isFirst, out string? remaining)
        {
            remaining = null;
            if (completeText.Length <= maxLength) return completeText;
            if (!isFirst) maxLength -= _userConfiguration.LongContentThreadOptions.Prefix.Length;
            bool isLast = completeText.Length <= maxLength;

            if (!isLast) maxLength -= _userConfiguration.LongContentThreadOptions.Suffix.Length;

            int lastSpace;
            for (lastSpace = maxLength; lastSpace >= _config.App.MinSplitLength; lastSpace--)
            {
                if (completeText[lastSpace] == ' ')

                    break;
            }

            string chunk = completeText[..lastSpace].TrimEnd();
            if (!isFirst) chunk = _userConfiguration.LongContentThreadOptions.Prefix + chunk;
            if (!isLast) chunk += _userConfiguration.LongContentThreadOptions.Suffix;
            remaining = completeText[lastSpace..].TrimStart();
            return chunk;
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

        public async Task Publish(Mastonet.Entities.Status toot)
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
           await Tweet(tootContent);

        }

        public async Task PublishFromToot(string content)
        {
            var replies = GetReplies(content, out string mainTweet);
            if (replies != null)
            {
                switch (_userConfiguration.LongContent)
                {
                    case LongContent.DontPublish:
                        break;

                    case LongContent.Cut:
                        await Tweet( mainTweet);
                        break;

                    case LongContent.Thread:
                        var tweet = await Tweet( mainTweet);

                        if (tweet.Id != 0)
                        {
                            foreach (var reply in replies)
                            {
                                tweet = await Tweet( reply, tweet.Id);
                            }
                        }
                        break;
                }
            }
            else
            {
                await Tweet(mainTweet);
            }
        }

        public async Task<ITweet> Tweet(string content, long? replyTo = null)
        {
            try
            {
                return await _userClient.Tweets.PublishTweetAsync(new PublishTweetParameters
                {
                    Text = content,
                    InReplyToTweetId = replyTo,
                });
            }
            catch (Exception ex)
            {
                // TODO: LOG
                return null;
            }
        }

        public async Task<bool> FinishAuthentication(string query)
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
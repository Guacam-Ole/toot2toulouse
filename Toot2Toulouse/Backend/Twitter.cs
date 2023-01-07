using System.Linq.Expressions;

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

        public Twitter(Interfaces.IConfig tootConfiguration)
        {
            _config = tootConfiguration.GetConfig();
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
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

        private List<string>? GetReplies(string originalToot, UserConfiguration userConfiguration, out string mainTweet)
        {
            DoReplacements(ref originalToot, userConfiguration);
            int maxLength = _config.App.TwitterCharacterLimit;
            bool addSuffix = true;

            string suffix = userConfiguration.AppSuffix.Content ?? string.Empty;

            bool needsSplit = originalToot.Length > maxLength;
            if (!needsSplit && originalToot.Length + suffix.Length > maxLength && userConfiguration.AppSuffix.HideOnLongText) addSuffix = false;
            mainTweet = originalToot;
            if (addSuffix) mainTweet += suffix;

            if (!needsSplit) return null;

            var replylist = new List<string>();
            mainTweet = GetChunk(mainTweet, maxLength, true, userConfiguration, out string? replies);
            while (replies != null)
            {
                replylist.Add(GetChunk(replies, maxLength, false, userConfiguration, out replies));
            }

            return replylist;
        }

        private void DoReplacements(ref string texttopublish, UserConfiguration userConfiguration)
        {
            texttopublish = $" {texttopublish} ";
            foreach (var translation in userConfiguration.Replacements)
            {
                texttopublish = texttopublish.Replace($" {translation.Key} ", $" {translation.Value} ", StringComparison.CurrentCultureIgnoreCase);
            }
            texttopublish = texttopublish.Trim();
        }

        private string GetChunk(string completeText, int maxLength, bool isFirst, UserConfiguration userConfiguration, out string? remaining)
        {
            remaining = null;
            if (completeText.Length <= maxLength) return completeText;
            if (!isFirst) maxLength -= userConfiguration.LongContentThreadOptions.Prefix.Length;
            bool isLast = completeText.Length <= maxLength;

            if (!isLast) maxLength -= userConfiguration.LongContentThreadOptions.Suffix.Length;


            int lastChar = maxLength;
            for (int lastSpace = maxLength; lastSpace >= _config.App.MinSplitLength; lastSpace--)
            {
                if (completeText[lastChar] == ' ') break;
            }

            string chunk = completeText.Substring(0, lastChar).TrimEnd();
            if (!isFirst) chunk = userConfiguration.LongContentThreadOptions.Prefix + chunk;
            if (!isLast) chunk = chunk + userConfiguration.LongContentThreadOptions.Suffix;
            remaining = completeText.Substring(lastChar).TrimStart();
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


        public async Task PublishFromToot(string content, TwitterClient userClient, UserConfiguration userConfiguration)
        {
            var replies = GetReplies(content, userConfiguration, out string mainTweet);
            if (replies != null)
            {
                switch (userConfiguration.LongContent)
                {
                    case LongContent.DontPublish:
                        break;
                    case LongContent.Cut:
                        await Tweet(userClient, mainTweet);
                        break;
                    case LongContent.Thread:
                        var tweet = await Tweet(userClient, mainTweet);

                        if (tweet.Id != 0)
                        {

                            foreach (var reply in replies)
                            {
                                tweet = await Tweet(userClient, reply, tweet.Id);
                            }
                        }
                        break;
                }
            }
        }

        public async Task<ITweet> Tweet(TwitterClient userClient, string content, long? replyTo = null)
        {
            try
            {
                return await userClient.Tweets.PublishTweetAsync(new PublishTweetParameters
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

            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();

            string suffix = " [🐘²🐦]";
            //    await Tweet(userClient, "Nur ein simpler Testtweet via API. Einfach ignorieren :)   " + suffix);

            string longText = "Das ist ein langer Thread. Bitte ignorieren..." +
                @"Rope's end gangplank hang the jib squiffy warp doubloon bilge rat hulk reef scuttle. Haul wind belay Sea Legs tender maroon rigging skysail jack knave holystone. Ho lugger transom Yellow Jack gaff Jolly Roger fire in the hole topmast ballast Barbary Coast.

Jack Tar killick fathom Admiral of the Black quarter hearties hempen halter ahoy careen mizzen. Pinnace hang the jib aft grog blossom plunder ye log reef sails lass rutters. Aft bucko lad rutters scallywag trysail handsomely galleon lass Buccaneer.

Yawl lateen sail carouser smartly fire ship pirate Nelsons folly league code of conduct Sea Legs. Scuppers driver loaded to the gunwalls Arr gangplank Sink me poop deck pillage lugger snow. Hempen halter bounty crimp come about grog blossom pink barque galleon wherry cable. ";
            await PublishFromToot(longText, userClient, _config.Defaults);
            return true;
        }
    }
}

using Mastonet.Entities;

using Newtonsoft.Json;

using System;
using System.Drawing;
using System.Net.Http;
using System.Net.NetworkInformation;

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
        private IAuthenticatedUser _twitterUser;
        private UserConfiguration _userConfiguration;
        private Account _mastonUser;
        private static readonly IAuthenticationRequestStore _twitterRequestStore = new LocalAuthenticationRequestStore();
        private readonly ILogger<Twitter> _logger;
        private readonly Interfaces.IMessage _toot;
        private readonly INotification _notification;

        public Twitter(ILogger<Twitter> logger, ConfigReader configReader, Interfaces.IMessage toot, INotification notification)
        {
            _config = configReader.Configuration;
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
            _logger = logger;
            _toot = toot;
            _notification = notification;
        }

        public async Task InitUserAsync(TwitterClient client, UserConfiguration userConfiguration)
        {
            try
            {
                _userClient = client;
                _twitterUser = await _userClient.Users.GetAuthenticatedUserAsync();
                _userConfiguration = userConfiguration;
                _toot.InitUser(userConfiguration);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing user", ex);
                throw;
            }
        }

        public async Task<string> GetAuthenticationUrlAsync(string baseUrl)
        {
            try
            {
                var guid = Guid.NewGuid();
                var targetUrl = baseUrl + "/twitter/code";
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

        private bool ShouldITweetThis(Mastonet.Entities.Status toot)
        {
            // TODO: CHeck visibilitysettings
            return !string.IsNullOrWhiteSpace(toot.Content);
        }

        public async Task PublishAsync(Mastonet.Entities.Status toot)
        {
            _mastonUser = toot.Account;
            if (!ShouldITweetThis(toot)) return;

            string contentWarning = _toot.StripHtml(toot.SpoilerText);
            bool hasContentWarning = !string.IsNullOrWhiteSpace(contentWarning);
            //if (toot.MediaAttachments != null && toot.MediaAttachments.Count() > 0)
            //{
            //    tootContent += "\n";
            //    foreach (var attachment in toot.MediaAttachments)
            //    {
            //        //   tootContent += attachment.Url + "\n";
            //    }
            //}
            await PublishFromToot(toot);
        }

        public async Task PublishFromToot(Status toot)
        {
            try
            {
                bool isSensitive = toot.Sensitive ?? false;
                if (!string.IsNullOrWhiteSpace(toot.SpoilerText))
                {
                    toot.Sensitive = true;
                    toot.Text = $"CW: {toot.SpoilerText}\n\n{toot.Text}";
                }

                string content = _toot.StripHtml(toot.Content);

                var replies = _toot.GetReplies(content, out string mainTweet);
                if (replies != null)
                {
                    switch (_userConfiguration.LongContent)
                    {
                        case ITwitter.LongContent.DontPublish:
                            _logger.LogDebug("didnt tweet for {twitterUser} because {contentLength} was more than the allowed twitter limit", _twitterUser, content.Length);
                            break;

                        case ITwitter.LongContent.Cut:
                            await TweetAsync(mainTweet, isSensitive, toot.MediaAttachments);
                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars cutting after {tweetLength} chars", _twitterUser, content.Length, mainTweet.Length);
                            break;

                        case ITwitter.LongContent.Thread:
                            var tweet = await TweetAsync(mainTweet, isSensitive, toot.MediaAttachments);

                            if (tweet.Id != 0)
                            {
                                foreach (var reply in replies)
                                {
                                    tweet = await TweetAsync(reply, isSensitive, tweet.Id);
                                }
                            }
                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars resulting in thread with {replyCount} replies", _twitterUser, content.Length, replies.Count);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    await TweetAsync(mainTweet, isSensitive, toot.MediaAttachments);
                    _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars ", _twitterUser, content.Length);
                }
            }
            catch (Exception)
            {
                _notification.Error(_mastonUser.Id, TootConfigurationApp.MessageCodes.TwitterDown, "Sorry. Could not send your tweet. Will NOT try again");
                // TODO: Retry
                throw;
            }
        }

        private async Task<IMedia> ValidateAndDownloadAttachmentAsync(Attachment attachment)
        {
            try
            {
                var fileInfo = new FileInfo(attachment.Url);
                byte[] fileContents;
                IMedia mediafile;

                switch (fileInfo.Extension)
                {

                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".webp":
                        fileContents = await DownloadFile(attachment.Url, attachment.PreviewUrl, _config.App.MaxImageSize);
                        mediafile = await _userClient.Upload.UploadTweetImageAsync( fileContents);
                        mediafile.Name = "Das ist ein tolles bild";
                        return mediafile;
                    case ".gif":
                        fileContents = await DownloadFile(attachment.Url, attachment.PreviewUrl, _config.App.MaxGifSize);
                        return await _userClient.Upload.UploadTweetImageAsync(fileContents);
                    case ".mp4":
                        fileContents = await DownloadFile(attachment.Url, null, _config.App.MaxVideoSize);
                        mediafile = await _userClient.Upload.UploadTweetVideoAsync(fileContents);
                        mediafile.Name = "Das ist ein tolles video";
                        return mediafile;
                    default:
                        throw new NotImplementedException(); // TODO: Own exceptiontype
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("oops", ex);
                return null;
            }
        }

        private async Task<ITweet> TweetAsync(string content, bool isSensitive, IEnumerable<Attachment> attachments)
        {
            var mediaFiles = new List<IMedia>();
            foreach (var attachment in attachments)
            {
                var mediafile = await ValidateAndDownloadAttachmentAsync(attachment);
                if (mediafile != null) mediaFiles.Add(mediafile);

            }

            return await TweetAsync(new PublishTweetParameters
            {
                Text = content,
                PossiblySensitive = isSensitive,
                Medias = mediaFiles
            });
        }

        private async Task<Stream> DownloadFile(string url)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<Byte[]> DownloadFile(string url, string? alternativeUrl, long maxSizeMegaBytes)
        {
            var urlStream = await DownloadFile(url);

            var sizeInMb = urlStream.Length / 1024d / 1024d;
            if (sizeInMb > maxSizeMegaBytes)
            {
                _logger.LogWarning("File at '{'url'} is too big. Allowed: {maxLength}. Filesize: {length}", url, maxSizeMegaBytes, sizeInMb);
                if (alternativeUrl != null)
                {
                    _logger.LogInformation("Will use previewurl instead");
                    urlStream = await DownloadFile(alternativeUrl);
                }
                else throw new IndexOutOfRangeException("File too big");     // TODO: Own exception type
            }

            var memoryStream = new MemoryStream();
            urlStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<ITweet> TweetAsync(string content, bool isSensitive, long replyTo)
        {
            return await TweetAsync(new PublishTweetParameters
            {
                Text = content,
                InReplyToTweetId = replyTo,
                PossiblySensitive = isSensitive
            });
        }

        public async Task<ITweet> TweetAsync(PublishTweetParameters tweetParameters)
        {
            try
            {
                return await _userClient.Tweets.PublishTweetAsync(tweetParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tweeting for {_twitterUser}", ex);
                throw;
            }

        }

        //private async Task<ITweet> TweetAsync(Status toot,  long? replyTo = null)
        //{
        //    bool isSensitive = toot.Sensitive ?? false;
        //    isSensitive = isSensitive || !string.IsNullOrWhiteSpace(toot.SpoilerText);

        //    var tweetParameters = new PublishTweetParameters
        //    {
        //        Text = toot.Text,
        //        InReplyToTweetId = replyTo,
        //        PossiblySensitive = isSensitive
        //    };

        //    try
        //    {
        //        return await _userClient.Tweets.PublishTweetAsync(new PublishTweetParameters
        //        {
        //            Text = content,
        //            InReplyToTweetId = replyTo,
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Error tweeting for {_twitterUser}", ex);
        //        throw;
        //    }
        //}

        public async Task<bool> FinishAuthenticationAsync(string query)
        {
            // TODO: Save credentials; Success page
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

            InitUserAsync(userClient, _config.Defaults);
            await PublishFromToot(new Status { Content = longText });
            return true;
        }
    }
}
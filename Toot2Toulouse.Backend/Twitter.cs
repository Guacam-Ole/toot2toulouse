using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Net.Http;
using System.Net.NetworkInformation;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

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
        private static readonly IAuthenticationRequestStore _twitterRequestStore = new LocalAuthenticationRequestStore();
        private readonly ILogger<Twitter> _logger;
        private readonly Interfaces.IMessage _toot;
        private readonly INotification _notification;
        private readonly ICookies _cookies;
        private readonly IDatabase _database;

        public Twitter(ILogger<Twitter> logger, ConfigReader configReader, Interfaces.IMessage toot, INotification notification, ICookies cookies, IDatabase database)
        {
            _config = configReader.Configuration;
            _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret);
            _logger = logger;
            _toot = toot;
            _notification = notification;
            _cookies = cookies;
            _database = database;
        }

     

        public async Task InitUserAsync(UserData userdata)
        {
            
            try
            {
                _userClient = new TwitterClient(userdata.Twitter.ConsumerKey, userdata.Twitter.ConsumerSecret, userdata.Twitter.AccessToken, userdata.Twitter.AccessSecret);
                _twitterUser = await _userClient.Users.GetAuthenticatedUserAsync();
                _userConfiguration = userdata.Config;
                _toot.InitUser(_userConfiguration);
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

        //private void TestAuthenticate()
        //{
        //    _appClient = new TwitterClient(_config.Secrets.Twitter.Consumer.ApiKey, _config.Secrets.Twitter.Consumer.ApiKeySecret, _config.Secrets.Twitter.Personal.AccessToken, _config.Secrets.Twitter.Personal.AccessTokenSecret);
        //}

       

        private bool ShouldITweetThis(Status toot)
        {
            // TODO: CHeck visibilitysettings
            return !string.IsNullOrWhiteSpace(toot.Content);
        }

        public async Task PublishAsync(Status toot)
        {
            if (!ShouldITweetThis(toot)) return;

            //string contentWarning = _toot.StripHtml(toot.SpoilerText);
            //bool hasContentWarning = !string.IsNullOrWhiteSpace(contentWarning);
          
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
                
                //_notification.Error(_mastonUser.Id, TootConfigurationApp.MessageCodes.TwitterDown, "Sorry. Could not send your tweet. Will NOT try again");
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

        public async Task<bool> FinishAuthenticationAsync(string query)
        {
            var hash = _cookies.UserHashGetCookie();
            var id=_cookies.UserIdGetCookie();

            if (id == Guid.Empty || hash == null) throw new Exception("No cookie. You shouldn't even be here");

            var t2tUser=_database.GetUserByIdAndHash(id, hash);
            if (t2tUser == null) throw new Exception("invalid cookie data");


            var requestParameters = await RequestCredentialsParameters.FromCallbackUrlAsync(query, _twitterRequestStore);
            var userCredentials = await _appClient.Auth.RequestCredentialsAsync(requestParameters);


            var credsStr = JsonConvert.SerializeObject(userCredentials);
            var userClient = new TwitterClient(userCredentials);
            var user = await userClient.Users.GetAuthenticatedUserAsync();
            t2tUser.Twitter = new Models.Twitter
            {
                ConsumerKey = userCredentials.ConsumerKey,
                ConsumerSecret = userCredentials.ConsumerSecret,
                AccessToken= userCredentials.AccessToken,
                AccessSecret= userCredentials.AccessTokenSecret,
                Bearer=userCredentials.BearerToken, 
                Id = user.Id.ToString(),
                DisplayName = user.Name,
                Handle=user.ScreenName
            };
            _database.UpsertUser(t2tUser);  

            _notification.Info(t2tUser.Id, TootConfigurationApp.MessageCodes.RegistrationFinished);

            InitUserAsync(t2tUser);
            //await PublishFromToot(new Status { Content = longText });
            return true;
        }
    }
}
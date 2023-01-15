using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

namespace Toot2Toulouse.Backend
{
    public class Twitter : ITwitter
    {
        private TootConfiguration _config;

        private readonly ILogger<Twitter> _logger;
        private readonly Interfaces.IMessage _toot;
        private readonly INotification _notification;
        private readonly IDatabase _database;

        public Twitter(ILogger<Twitter> logger, ConfigReader configReader, Interfaces.IMessage toot, INotification notification, IDatabase database)
        {
            _config = configReader.Configuration;
            _logger = logger;
            _toot = toot;
            _notification = notification;
            _database = database;
        }

        private TwitterClient GetUserClient(UserData userData)
        {
            return new TwitterClient(userData.Twitter.ConsumerKey, userData.Twitter.ConsumerSecret, userData.Twitter.AccessToken, userData.Twitter.AccessSecret);
        }

        private async Task<IAuthenticatedUser> GetTwitterUser(UserData userData)
        {
            return await GetUserClient(userData).Users.GetAuthenticatedUserAsync();
        }

        private bool ShouldITweetThis(Status toot)
        {
            // TODO: CHeck visibilitysettings
            return !string.IsNullOrWhiteSpace(toot.Content);
        }

        public async Task<List<long>> PublishAsync(UserData userData, Status toot)
        {
            if (!ShouldITweetThis(toot)) return new List<long>();
            return await PublishFromToot(userData, toot);
        }

        private async Task<List<long>> PublishFromToot(UserData userData, Status toot)
        {
            var tweetIds=new    List<long>();
            try
            {
                bool isSensitive = toot.Sensitive ?? false;
                if (!string.IsNullOrWhiteSpace(toot.SpoilerText))
                {
                    toot.Sensitive = true;
                    toot.Text = $"CW: {toot.SpoilerText}\n\n{toot.Text}";
                }

                string content = _toot.StripHtml(toot.Content);
                var twitterUser = GetTwitterUser(userData);

                var replies = _toot.GetReplies(userData.Config, content, out string mainTweet);
                if (replies != null)
                {
                    switch (userData.Config.LongContent)
                    {
                        case ITwitter.LongContent.DontPublish:
                            _logger.LogDebug("didnt tweet for {twitterUser} because {contentLength} was more than the allowed twitter limit", twitterUser, content.Length);
                            break;

                        case ITwitter.LongContent.Cut:
                            var cuttweet= await TweetAsync(userData, mainTweet, isSensitive, toot.MediaAttachments);
                            tweetIds.Add(cuttweet.Id);

                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars cutting after {tweetLength} chars", twitterUser, content.Length, mainTweet.Length);
                            break;

                        case ITwitter.LongContent.Thread:
                            var tweet = await TweetAsync(userData, mainTweet, isSensitive, toot.MediaAttachments);

                            if (tweet.Id != 0)
                            {
                                tweetIds.Add(tweet.Id);
                                foreach (var reply in replies)
                                {
                                    tweet = await TweetAsync(userData, reply, isSensitive, tweet.Id);
                                    tweetIds.Add(tweet.Id);
                                }
                            }
                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars resulting in thread with {replyCount} replies", twitterUser, content.Length, replies.Count);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    var singletweet= await TweetAsync(userData, mainTweet, isSensitive, toot.MediaAttachments);
                    tweetIds.Add(singletweet.Id);
                    _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars ", twitterUser, content.Length);
                }
                return tweetIds;
            }
            catch (Exception)
            {
                //_notification.Error(_mastonUser.Id, TootConfigurationApp.MessageCodes.TwitterDown, "Sorry. Could not send your tweet. Will NOT try again");
                // TODO: Retry
                throw;
            }
        }

        private async Task<IMedia> ValidateAndDownloadAttachmentAsync(UserData userData, Attachment attachment)
        {
            try
            {
                var userClient = GetUserClient(userData);
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
                        mediafile = await userClient.Upload.UploadTweetImageAsync(fileContents);
                        mediafile.Name = "Das ist ein tolles bild";
                        return mediafile;

                    case ".gif":
                        fileContents = await DownloadFile(attachment.Url, attachment.PreviewUrl, _config.App.MaxGifSize);
                        return await userClient.Upload.UploadTweetImageAsync(fileContents);

                    case ".mp4":
                        fileContents = await DownloadFile(attachment.Url, null, _config.App.MaxVideoSize);
                        mediafile = await userClient.Upload.UploadTweetVideoAsync(fileContents);
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

        private async Task<ITweet> TweetAsync(UserData userData, string content, bool isSensitive, IEnumerable<Attachment> attachments)
        {
            var mediaFiles = new List<IMedia>();
            foreach (var attachment in attachments)
            {
                var mediafile = await ValidateAndDownloadAttachmentAsync(userData, attachment);
                if (mediafile != null) mediaFiles.Add(mediafile);
            }

            return await TweetAsync(userData, new PublishTweetParameters
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

        public async Task<ITweet> TweetAsync(UserData userData, string content, bool isSensitive, long replyTo)
        {
            return await TweetAsync(userData, new PublishTweetParameters
            {
                Text = content,
                InReplyToTweetId = replyTo,
                PossiblySensitive = isSensitive
            });
        }

        public async Task<ITweet> TweetAsync(UserData userData, PublishTweetParameters tweetParameters)
        {
            try
            {
                return await GetUserClient(userData).Tweets.PublishTweetAsync(tweetParameters);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tweeting for {userData.Twitter.DisplayName}", ex);
                throw;
            }
        }
    }
}
using Mastonet.Entities;

using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi;
using Tweetinvi.Exceptions;
using Tweetinvi.Logic.QueryParameters;
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
        private Dictionary<string, string> _globalReplacements = null;

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

        private async Task<IAuthenticatedUser> GetTwitterUserAsync(UserData userData)
        {
            return await GetUserClient(userData).Users.GetAuthenticatedUserAsync();
        }

        private bool ShouldITweetThis(UserData user, Status toot)
        {
            if (string.IsNullOrWhiteSpace(toot.Content)) return false;
            if (!user.Config.VisibilitiesToPost.Contains(toot.Visibility.ToT2t())) return false;
            if (user.Config.DontTweet.Any(q => toot.Content.Contains(q))) return false;
            return true;
        }

        public async Task<List<long>> PublishAsync(UserData userData, Status toot, long? replyTo = null)
        {
            if (!ShouldITweetThis(userData, toot))
            {
                _logger.LogTrace("Didn't tweet toot {id} ", toot.Id);
                return new List<long>();
            }
            return await PublishFromTootAsync(userData, toot, replyTo);
        }

        private async Task FillGlobalReplacements()
        {
            if (_globalReplacements == null)
            {
                _globalReplacements = new Dictionary<string, string>();
                var allusers = await _database.GetAllUsers();

                foreach (var user in allusers)
                {
                    if (user.Config.Replacements == null) continue;
                    var userReplacements = user.Config.Replacements.Where(q => q.Key.StartsWith("@"));
                    if (userReplacements.Any())
                    {
                        foreach (var replacement in userReplacements)
                        {
                            if (_globalReplacements.ContainsKey(replacement.Key)) continue;
                            _globalReplacements.Add(replacement.Key, replacement.Value);
                        }
                    }
                }
            }
        }

        private void ReplaceContent(IEnumerable<Mention> mentions, UserData userdata, ref string content)
        {
            content = $" {content} ";
            if (mentions == null) return;
            var personalUserReplacements = userdata.Config.Replacements.Where(q => q.Key.StartsWith("@"));
            var personalNonUserReplacements = userdata.Config.Replacements.Where(q => !q.Key.StartsWith("@"));
            var globalUserReplacements = new Dictionary<string, string>();
            if (userdata.Config.UseGlobalMentions && _globalReplacements != null) globalUserReplacements = _globalReplacements;

            foreach (var mention in mentions)
            {
                var userReplacement = personalUserReplacements.ReplacementForUser(mention);
                var globalUserReplacement = globalUserReplacements.ReplacementForUser(mention);

                if (userReplacement != null)
                {
                    content = content.Replace(mention, userReplacement);
                    continue;
                }
                else if (globalUserReplacement != null)
                {
                    content = content.Replace(mention, globalUserReplacement);
                    continue;
                }

                content = content.Replace(mention, $"🐘{mention.UserName}");
            }

            foreach (var nonUserReplacement in personalNonUserReplacements)
            {
                content = content.Replace(nonUserReplacement);
            }
            content = content.Trim();
        }

        private async Task<List<long>> PublishFromTootAsync(UserData userData, Status toot, long? replyTo)
        {
            var tweetIds = new List<long>();
            try
            {
                if (!string.IsNullOrWhiteSpace(toot.SpoilerText))
                {
                    toot.Sensitive = true;
                    toot.Content = $"CW: {toot.SpoilerText}\n\n{toot.Content}";
                }
                bool isSensitive = toot.Sensitive ?? false;

                string content = toot.Content.StripHtml();
                if (userData.Config.UseGlobalMentions) await FillGlobalReplacements();
                ReplaceContent(toot.Mentions, userData, ref content);

                var twitterUser = await GetTwitterUserAsync(userData);

                var replies = _toot.GetReplies(userData.Config, content, out string mainTweet);
                if (replies != null)
                {
                    switch (userData.Config.LongContent)
                    {
                        case ITwitter.LongContent.DontPublish:
                            _logger.LogDebug("didnt tweet for {twitterUser} because {contentLength} was more than the allowed twitter limit", twitterUser.ScreenName, content.Length);
                            break;

                        case ITwitter.LongContent.Cut:
                            var cuttweet = await TweetAsync(userData, mainTweet, isSensitive, replyTo, toot.MediaAttachments);
                            tweetIds.Add(cuttweet.Id);

                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars cutting after {tweetLength} chars", twitterUser.ScreenName, content.Length, mainTweet.Length);
                            break;

                        case ITwitter.LongContent.Thread:
                            var tweet = await TweetAsync(userData, mainTweet, isSensitive, replyTo, toot.MediaAttachments);

                            if (tweet.Id != 0)
                            {
                                tweetIds.Add(tweet.Id);
                                foreach (var replyContent in replies)
                                {
                                    tweet = await TweetAsync(userData, replyContent, isSensitive, tweet.Id, null);
                                    if (tweet == null) ;
                                    tweetIds.Add(tweet.Id);
                                }
                            }
                            _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars resulting in thread with {replyCount} replies", twitterUser.ScreenName, content.Length, replies.Count);
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    var singletweet = await TweetAsync(userData, mainTweet, isSensitive, replyTo, toot.MediaAttachments);
                    tweetIds.Add(singletweet.Id);
                    _logger.LogDebug("tweeted for {twitterUser} containing {contentLength} chars ", twitterUser.ScreenName, content.Length);
                }
                return tweetIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "error tweeting");
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
                        fileContents = await DownloadFileAsync(attachment.Url, attachment.PreviewUrl, _config.App.MaxImageSize);
                        mediafile = await userClient.Upload.UploadTweetImageAsync(fileContents);

                        break;

                    case ".gif":
                        fileContents = await DownloadFileAsync(attachment.Url, attachment.PreviewUrl, _config.App.MaxGifSize);
                        mediafile = await userClient.Upload.UploadTweetImageAsync(fileContents);
                        break;

                    case ".mp4":
                        fileContents = await DownloadFileAsync(attachment.Url, null, _config.App.MaxVideoSize);
                        mediafile = await userClient.Upload.UploadTweetVideoAsync(fileContents);
                        break;

                    default:
                        throw new NotImplementedException(); // TODO: Own exceptiontype
                }
                if (mediafile != null && attachment.Description != null)
                {
                    await userClient.Upload.AddMediaMetadataAsync(new MediaMetadata(mediafile)
                    {
                        AltText = attachment.Description
                    });
                }
                return mediafile;
            }
            catch (Exception ex)
            {
                _logger.LogError("Downloading attachment failed. Will tweet without attachment", ex);
                return null;
            }
        }

        private async Task<ITweet> TweetAsync(UserData userData, string content, bool isSensitive, long? replyTo, IEnumerable<Attachment> attachments = null)
        {
            var tweetParameters = new PublishTweetParameters
            {
                Text = content,
                PossiblySensitive = isSensitive,
                InReplyToTweetId = replyTo,
                AutoPopulateReplyMetadata = true
            };

            if (attachments?.Count() > 0)
            {
                var mediaFiles = new List<IMedia>();
                foreach (var attachment in attachments)
                {
                    var mediafile = await ValidateAndDownloadAttachmentAsync(userData, attachment);
                    if (mediafile != null) mediaFiles.Add(mediafile);
                }
                tweetParameters.Medias = mediaFiles;
            }

            return await TweetAsync(userData, tweetParameters);
        }

        private async Task<Stream> DownloadFileAsync(string url)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        private async Task<byte[]> DownloadFileAsync(string url, string? alternativeUrl, long maxSizeMegaBytes)
        {
            var urlStream = await DownloadFileAsync(url);

            var sizeInMb = urlStream.Length / 1024d / 1024d;
            if (sizeInMb > maxSizeMegaBytes)
            {
                _logger.LogWarning("File at '{'url'} is too big. Allowed: {maxLength}. Filesize: {length}", url, maxSizeMegaBytes, sizeInMb);
                if (alternativeUrl != null)
                {
                    _logger.LogInformation("Will use previewurl instead");
                    urlStream = await DownloadFileAsync(alternativeUrl);
                }
                else throw new IndexOutOfRangeException("File too big");     // TODO: Own exception type
            }

            var memoryStream = new MemoryStream();
            urlStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }

        private async Task<ITweet> TweetAsync(UserData userData, PublishTweetParameters tweetParameters)
        {
            try
            {
                return await GetUserClient(userData).Tweets.PublishTweetAsync(tweetParameters);
            }
            catch (TwitterException tex) // when (tex.StatusCode==403)
            {
                _logger.LogWarning(tex, "Tweet-error {text} to {reply} ", tweetParameters.Text, tweetParameters.InReplyToTweet);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error tweeting for {userData.Twitter.DisplayName}", ex);
                throw;
            }
        }
    }
}
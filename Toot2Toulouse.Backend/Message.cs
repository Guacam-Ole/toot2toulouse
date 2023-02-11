using Microsoft.Extensions.Logging;

using System.Text.RegularExpressions;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class Message : IMessage
    {
        private readonly ILogger<Message> _logger;

        private readonly TootConfiguration _config;

        public Message(ILogger<Message> logger, ConfigReader configReader)
        {
            _logger = logger;
            _config = configReader.Configuration;
        }

    
        public List<string>? GetReplies(UserConfiguration userConfiguration, string originalToot, out string mainTweet)
        {
            DoReplacements(userConfiguration, ref originalToot);
            int maxLength = _config.App.TwitterCharacterLimit;
            bool addSuffix = true;

            string suffix = userConfiguration.AppSuffix.Content ?? string.Empty;
            if (!string.IsNullOrEmpty(suffix)) suffix = "\n" + suffix;

            bool needsSplit = originalToot.Length > maxLength;
            if (!needsSplit && originalToot.Length + suffix.Length > maxLength && userConfiguration.AppSuffix.HideIfBreaks) addSuffix = false;
            mainTweet = originalToot;
            if (addSuffix) mainTweet += suffix;

            if (!needsSplit) return null;

            var replylist = new List<string>();
            mainTweet = GetChunk(userConfiguration, mainTweet, maxLength, true, out string? replies);
            while (replies != null)
            {
                replylist.Add(GetChunk(userConfiguration, replies, maxLength, false, out replies));
            }

            return replylist;
        }

        private void DoReplacements(UserConfiguration userConfiguration, ref string texttopublish)
        {
            texttopublish = $" {texttopublish} ";
            foreach (var translation in userConfiguration.Replacements)
            {
                texttopublish = texttopublish.Replace($" {translation.Key} ", $" {translation.Value} ", StringComparison.CurrentCultureIgnoreCase);
            }
            texttopublish = texttopublish.Trim();
        }

        private string GetChunk(UserConfiguration userConfiguration, string completeText, int maxLength, bool isFirst, out string? remaining)
        {
            remaining = null;
            if (completeText.Length <= maxLength)
            {
                if (!isFirst && completeText.Length + userConfiguration.LongContentThreadOptions.Prefix.Length <= maxLength) return userConfiguration.LongContentThreadOptions.Prefix + completeText;
                return completeText;
            }
            if (!isFirst) maxLength -= userConfiguration.LongContentThreadOptions.Prefix.Length;
            bool isLast = completeText.Length <= maxLength;

            if (!isLast) maxLength -= userConfiguration.LongContentThreadOptions.Suffix.Length;

            int lastSpace;
            for (lastSpace = maxLength; lastSpace >= _config.App.MinSplitLength; lastSpace--)
            {
                if (completeText[lastSpace] == ' ')

                    break;
            }

            string chunk = completeText[..lastSpace].TrimEnd();
            if (!isFirst) chunk = userConfiguration.LongContentThreadOptions.Prefix + chunk;
            if (!isLast) chunk += userConfiguration.LongContentThreadOptions.Suffix;
            remaining = completeText[lastSpace..].TrimStart();
            return chunk;
        }
    }
}
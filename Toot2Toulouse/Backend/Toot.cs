using System.Text.RegularExpressions;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class Toot : IToot
    {
        private UserConfiguration _userConfiguration;
        private readonly ILogger<Toot> _logger;
        private readonly TootConfiguration _config;

        public Toot(ILogger<Toot> logger, ConfigReader configReader)
        {
            _logger = logger;
            _config = configReader.Configuration;
        }

        public void InitUser(UserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
        }

        public string StripHtml(string content)
        {
            content = content.Replace("</p>", "\n\n");
            content = content.Replace("<br />", "\n");
            return Regex.Replace(content, "<[a-zA-Z/].*?>", String.Empty);
        }

        public List<string>? GetReplies(string originalToot, out string mainTweet)
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
                replylist.Add(GetChunk(replies, maxLength, false, out replies));
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
    }
}
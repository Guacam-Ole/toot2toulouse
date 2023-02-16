using Mastonet.Entities;

using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Toot2Toulouse.Backend.Configuration;

using Tweetinvi.Streams.Helpers;

using static Toot2Toulouse.Backend.Configuration.UserConfiguration;

namespace Toot2Toulouse.Backend
{
    public static class Helpers
    {
        private static readonly string _urlReplacementString = $"[URL-NUM-{new string('*', 20)}]";

        public static T Clone<T>(this T source)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source, ConfigReader.JsonOptions));
        }

        public static string StripComments(this string json)
        {
            json = Regex.Replace(json, @"//(.*?)\r?\n", " \n", RegexOptions.Multiline);  // removes comments like this
            return json;
        }

        public static string StripHtml(this string content)
        {
            content = content.Replace("</p>", " \n\n");
            content = content.Replace("<br />", " \n");
            content = Regex.Replace(content, "<[a-zA-Z/].*?>", String.Empty);
            content = System.Net.WebUtility.HtmlDecode(content);
            return content;
        }


        public static void RemoveSecrets<T>(this T item)
        {
            if (item == null) return;
            foreach (var propertyInfo in item.GetType().GetProperties())
            {
                if (!propertyInfo.CanWrite || !propertyInfo.CanRead) continue;
                if (propertyInfo.GetCustomAttribute<HideOnExport>() != null)
                {
                    propertyInfo.SetValue(item, default);
                }
                if (propertyInfo.PropertyType.IsClass && propertyInfo.PropertyType.Namespace.StartsWith("Toot2Toulouse"))
                {
                    var value = propertyInfo.GetValue(item);
                    value?.RemoveSecrets();
                }
            }
        }

        public static string? ReplacementForUser(this IEnumerable<KeyValuePair<string, string>> replacements, Mention mention)
        {
            if (replacements == null) return null;
            var replacement = replacements.FirstOrDefault(q => q.Key.ToLower() == "@" + mention.AccountName.ToLower());
            return replacement.Value;
        }

        public static string Replace(this string content, Mention mention, string replacement)
        {
            return Replace(content, new KeyValuePair<string,string>("@"+mention.UserName, replacement));
        }

        public static string Replace(this string content, KeyValuePair<string,string> pair)
        {
            return
                content.Replace($" {pair.Key} ", $" {pair.Value} ", StringComparison.CurrentCultureIgnoreCase)
                .Replace($"\n{pair.Key} ", $"\n{pair.Value} ", StringComparison.CurrentCultureIgnoreCase);
        }


        public static Mastonet.Visibility ToMastonet(this Visibilities visibility)
        {
            return Enum.Parse<Mastonet.Visibility>(visibility.ToString());
        }

        public static Visibilities ToT2t(this Mastonet.Visibility visibility)
        {
            return Enum.Parse<Visibilities>(visibility.ToString());
        }

        public static T? SetLimits<T>(this T? value, T min, T max) where T : IComparable
        {
            if (value == null) return value;
            if (value.CompareTo(min) < 0) value = min;
            if (value.CompareTo(max) > 0) value = max;
            return value;
        }

        public static string Shorten(this string? value, int maxLength)
        {
            if (value == null) return string.Empty;
            if (value.Length > maxLength) return value[..maxLength];
            return value;
        }

        public static string GetUrlsInText(this string content,out IEnumerable<string> urls)
        {
            var words = content.Replace('\n',' ').Split(' ');
            urls = words.Where(q => q.StartsWith("http://") || q.StartsWith("https://"));
            if (!urls.Any()) return content;
            int count = 0;
            foreach (var url in urls)
            {
                content = content.Replace(url, _urlReplacementString.Replace("NUM", $"{count++:000}"));
            }
            return content;
        }

        public static string ReInsertUrls(this string content, IEnumerable<string>? urls)
        {
            if (urls== null) return content;
            int count = 0;
            foreach (var url in urls)
            {
                content = content.Replace(_urlReplacementString.Replace("NUM", $"{count++:000}"), url);
            }
            return content;
        }
    }
}

using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.UserConfiguration;

namespace Toot2Toulouse.Backend
{
    public static class Helpers
    {
        public static T Clone<T>(this T source)
        {
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source, ConfigReader.JsonOptions));
        }

        public static string StripComments(this string json)
        {

            json = Regex.Replace(json, @"//(.*?)\r?\n", "\n", RegexOptions.Multiline);  // removes comments like this
            return json;
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

        public static Mastonet.Visibility ToMastonet(this Visibilities visibility )
        {
            return Enum.Parse<Mastonet.Visibility>(visibility.ToString());
        }

        public static Visibilities ToT2t(this Mastonet.Visibility visibility)
        {
            return Enum.Parse<Visibilities>(visibility.ToString());
        }


        public static T? SetLimits<T>(this T? value, T min, T max) where T: IComparable
        {
            if (value == null) return value;  
            if (value.CompareTo(min) < 0) value = min;
            if (value.CompareTo(max)>0) value = max;   
            return value;   
        }

        public static string  Shorten(this string? value, int maxLength)
        {
            if (value == null) return string.Empty;
            if (value.Length>maxLength) return value[..maxLength];
            return value;
        }
    }
}

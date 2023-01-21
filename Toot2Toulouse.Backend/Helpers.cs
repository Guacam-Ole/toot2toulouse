
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
            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(source, new JsonSerializerOptions {  }));
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

    }
}

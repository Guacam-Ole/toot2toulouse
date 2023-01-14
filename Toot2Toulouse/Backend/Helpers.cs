using Newtonsoft.Json;

using System.Reflection;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend
{
    public static class Helpers
    {
        public static T Clone<T>(this T source)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
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

    }
}

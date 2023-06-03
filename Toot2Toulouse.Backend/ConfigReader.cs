
using System.Text.Json;
using Toot2Toulouse.Backend.Configuration;

using static Toot2Toulouse.Backend.Configuration.Messages;
using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

namespace Toot2Toulouse.Backend
{
    public class ConfigReader
    {
        public static JsonSerializerOptions JsonOptions=new JsonSerializerOptions {  PropertyNameCaseInsensitive=true, AllowTrailingCommas=true, WriteIndented=true };
        private readonly string _path;

        // private readonly IWebHostEnvironment _webHostEnvironment;
        public TootConfiguration Configuration { get; set; }

        public ConfigReader(string path)
        {

            try
            {
                _path = path;
                Configuration = GetApplicationConfig();
                Configuration.CurrentVersion = GetType().Assembly.GetName().Version;
                if (SecretsAreMissing())
                {
                    throw new Exception("Not all Secrets are configured");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed reading config");
                Console.WriteLine(ex);
                throw;
            }
        }

        private TootConfiguration GetApplicationConfig()
        {
            return ReadJsonFile<TootConfiguration>("config.json");
        }

        public T ReadJsonFile<T>(string filename)
        {
            string fullpath = Path.Combine(_path, filename);
            using var r = new StreamReader(fullpath);
            string json = r.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json.StripComments(), JsonOptions);
        }

        private static bool SecretsAreMissing(params string[] properties)
        {
            return properties.Any(p => string.IsNullOrEmpty(p));
        }

        private bool SecretsAreMissing()
        {
            bool twitterSecretsMissing = SecretsAreMissing(Configuration.Secrets.Twitter.Consumer.ApiKey, Configuration.Secrets.Twitter.Consumer.ApiKeySecret);
            bool mastodonSecretsMissing = SecretsAreMissing(Configuration.Secrets.Mastodon.AccessToken, Configuration.Secrets.Mastodon.ClientId, Configuration.Secrets.Mastodon.ClientSecret);
            return twitterSecretsMissing || mastodonSecretsMissing;
        }

        public Dictionary<MessageCodes, string> GetMessagesForLanguage(string language)
        {
            return ReadJsonFile<Dictionary<MessageCodes, string>>($"messages.{language}.json");
        }
    }
}
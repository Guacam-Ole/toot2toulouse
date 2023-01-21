
using System.Text.Json;
using Toot2Toulouse.Backend.Configuration;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

namespace Toot2Toulouse.Backend
{
    public class ConfigReader
    {
        private readonly string _path;

        // private readonly IWebHostEnvironment _webHostEnvironment;
        public TootConfiguration Configuration { get; set; }

        public ConfigReader(string path)
        {
            _path = path;
            //   _webHostEnvironment = webHostEnvironment;
            Configuration = GetApplicationConfig();
            Configuration.CurrentVersion = GetType().Assembly.GetName().Version;
            if (SecretsAreMissing())
            {
                throw new Exception("Not all Secrets are configured");
            }
        }

        private TootConfiguration GetApplicationConfig()
        {
            return ReadJsonFile<TootConfiguration>("config.json");
        }

        public T ReadJsonFile<T>(string filename)
        {
            string fullpath = Path.Combine(_path, filename);
            //_webHostEnvironment.ContentRootPath, "Properties", filename);
            using var r = new StreamReader(fullpath);
            string json = r.ReadToEnd();
            return JsonSerializer.Deserialize<T>(json.StripComments());
        }

        private static bool SecretsAreMissing(params string[] properties)
        {
            return properties.Any(p => string.IsNullOrEmpty(p));
        }

        private bool SecretsAreMissing()
        {
            bool twitterSecretsMissing = SecretsAreMissing(Configuration.Secrets.Twitter.Consumer.ApiKey, Configuration.Secrets.Twitter.Consumer.ApiKeySecret, Configuration.Secrets.Twitter.Personal.AccessToken, Configuration.Secrets.Twitter.Personal.AccessTokenSecret);
            bool mastodonSecretsMissing = SecretsAreMissing(Configuration.Secrets.Mastodon.AccessToken, Configuration.Secrets.Mastodon.ClientId, Configuration.Secrets.Mastodon.ClientSecret);
            return twitterSecretsMissing || mastodonSecretsMissing;
        }

        public Dictionary<MessageCodes, string> GetMessagesForLanguage(string language)
        {
            return ReadJsonFile<Dictionary<MessageCodes, string>>($"messages.{language}.json");
        }
    }
}
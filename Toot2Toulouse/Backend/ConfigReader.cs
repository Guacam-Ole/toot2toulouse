using Newtonsoft.Json;

using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend
{
    public class ConfigReader
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public TootConfiguration Configuration { get; set; }

        public ConfigReader(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
            Configuration = RetApplicationConfig();
            if (SecretsAreMissing())
            {
                throw new Exception("Not all Secrets are configured");
            }
        }

        private TootConfiguration RetApplicationConfig()
        {
            return ReadJsonFile<TootConfiguration>("config.json");
        }

        public T ReadJsonFile<T>(string filename)
        {
            string fullpath = Path.Combine(_webHostEnvironment.ContentRootPath, "Properties", filename);
            using (StreamReader r = new StreamReader(fullpath))
            {
                string json = r.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        private static bool SecretsAreMissing(params string[] properties)
        {
            return properties.Any(p => string.IsNullOrEmpty(p));
        }

        public bool SecretsAreMissing()
        {
            bool twitterSecretsMissing = SecretsAreMissing(Configuration.Secrets.Twitter.Consumer.ApiKey, Configuration.Secrets.Twitter.Consumer.ApiKeySecret, Configuration.Secrets.Twitter.Personal.AccessToken, Configuration.Secrets.Twitter.Personal.AccessTokenSecret);
            bool mastodonSecretsMissing = SecretsAreMissing(Configuration.Secrets.Mastodon.AccessToken, Configuration.Secrets.Mastodon.ClientId, Configuration.Secrets.Mastodon.ClientSecret);
            return twitterSecretsMissing|| mastodonSecretsMissing;
        }
    }
}
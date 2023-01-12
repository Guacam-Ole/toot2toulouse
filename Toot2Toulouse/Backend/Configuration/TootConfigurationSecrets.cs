namespace Toot2Toulouse.Backend.Configuration
{
    public class TootConfigurationSecrets
    {
        public SecretsTwitter Twitter { get; set; }
        public SecretsMastodon Mastodon { get; set; }  
        public string Salt { get; set; }
    }

    public class SecretsMastodon
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessToken { get; set; } 
    }

    public class SecretsTwitter
    {
        public SecretsTwitterConsumer Consumer { get; set; }
        public SecretsTwitterPersonal Personal { get; set; }
    }

    public class SecretsTwitterConsumer
    {
        public string ApiKey { get; set; }
        public string ApiKeySecret { get; set; }
    }

    public class SecretsTwitterPersonal
    {
        public string AccessToken { get; set; }
        public string AccessTokenSecret { get; set; }
    }
}
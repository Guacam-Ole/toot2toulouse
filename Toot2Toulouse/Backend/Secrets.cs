namespace Toot2Toulouse.Backend
{
    public class Secrets
    {
        public SecretsTwitter Twitter { get; set; }
    }

    public class SecretsTwitter
    {
        public string Bearer { get; set; }
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
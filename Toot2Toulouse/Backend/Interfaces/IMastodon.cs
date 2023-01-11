using Mastonet.Entities;

using System.Threading.Tasks;

using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IMastodon
    {
        public enum Visibilites
        {
            Public,
            NotListed,
            OnlyFollowers,
            OnlyMentioned
        }

        Task SendAllStatusMessagesToAsync(string recipient);
        //Task<SecretsMastodon> CreateNewAppAsync(TootConfigurationApp appConfig, SecretsMastodon mastodonSecrets);
        Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100);
        Task<string> GetAuthenticationUrl(string requestHost, string userInstance);
    }
}

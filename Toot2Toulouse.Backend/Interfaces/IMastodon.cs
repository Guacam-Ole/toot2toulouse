using Mastonet.Entities;

using static Toot2Toulouse.Backend.Configuration.TootConfigurationApp;

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


        Task<IEnumerable<Status>> GetServicePostsContainingAsync(string searchString, int limit = 100);

        Task<string> GetAuthenticationUrl(string requestHost, string userInstance);

        Task<KeyValuePair<bool, string>> UserIsAllowedToRegister(string userInstance, string verificationCode);
        Task SendStatusMessageTo(Guid id, string? prefix, MessageCodes messageCode);
    }
}
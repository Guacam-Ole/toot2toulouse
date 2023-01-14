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

        Task SendStatusMessageTo(Guid id, string? prefix, MessageCodes messageCode);

        Task<Account?> GetUserAccountByAccessToken(string instance, string accessToken);
    }
}
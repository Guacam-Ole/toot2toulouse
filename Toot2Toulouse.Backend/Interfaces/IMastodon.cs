using Mastonet;
using Mastonet.Entities;

using Toot2Toulouse.Backend.Models;

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

        Task<Account?> GetUserAccount(UserData userData);
        Task<Account?> GetUserAccount(MastodonClient mastodonClient);
        //Task<Status?> GetLatestToot(Guid id);
        Task<List<Status>> GetNonPostedToots(Guid id);
        //MastodonClient GetUserClient(UserData userData);
    }
}
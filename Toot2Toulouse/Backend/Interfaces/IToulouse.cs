using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        Task TweetServicePostsAsync();   // TODO: Remove when finished
        List<DisplaySettingsItem> GetServerSettingsForDisplay();
         void AddHashToUser(User user);
         User? GetUserByHash(Guid userId, string hash);

    }
}

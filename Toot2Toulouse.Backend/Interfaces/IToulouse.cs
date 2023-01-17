using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        //Task TweetServicePostsAsync();   // TODO: Remove when finished

        List<DisplaySettingsItem> GetServerSettingsForDisplay();
        Task SendTootsForAllUsers();
        Task GetTootsContaining(string mastodonHandle, string searchstring, int limit);

    }
}
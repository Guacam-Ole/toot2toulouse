using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        Task TweetServicePostsAsync();   // TODO: Remove when finished
        Task InitUserAsync(UserData userData);

        List<DisplaySettingsItem> GetServerSettingsForDisplay();


    }
}
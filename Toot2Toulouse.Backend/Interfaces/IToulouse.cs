using Mastonet.Entities;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        List<DisplaySettingsItem> GetServerSettingsForDisplay();

        Task SendTootsForAllUsers();

        Task<List<Status>> GetTootsContaining(string mastodonHandle, string searchstring, int limit);

        Task Invite(string mastodonHandle);
    }
}
using Mastonet.Entities;

using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        List<DisplaySettingsItem> GetServerSettingsForDisplay();

        Configuration.TootConfigurationAppModes.ValidModes GetServerMode();


        Task SendTootsForAllUsers();

        Task<List<Status>> GetTootsContaining(string mastodonHandle, string searchstring, int limit);

        Task Invite(string mastodonHandle);
        void CalculateServerStats();
    }
}
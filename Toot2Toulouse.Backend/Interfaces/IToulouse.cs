using Mastonet.Entities;

using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        List<DisplaySettingsItem> GetServerSettingsForDisplay();

        Task<Configuration.TootConfigurationAppModes.ValidModes> GetServerMode();

        Task SendTootsForAllUsersAsync();

        Task<List<Status>> GetTootsContainingAsync(string mastodonHandle, string searchstring, int limit);

        Task InviteAsync(string mastodonHandle);

        Task CalculateServerStats();

        Task SendSingleTootAsync(UserData user, string tootId);
    }
}
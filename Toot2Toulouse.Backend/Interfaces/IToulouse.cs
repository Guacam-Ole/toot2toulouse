using Mastonet.Entities;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToulouse
    {
        List<DisplaySettingsItem> GetServerSettingsForDisplay();

        Configuration.TootConfigurationAppModes.ValidModes GetServerMode();

        Task SendTootsForAllUsersAsync();

        Task<List<Status>> GetTootsContainingAsync(string mastodonHandle, string searchstring, int limit);

        Task InviteAsync(string mastodonHandle);

        void CalculateServerStats();

        Task SendSingleTootAsync(Guid userId, string tootId);
    }
}
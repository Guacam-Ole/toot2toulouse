using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface INotification
    {
        void Warning(long mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
        void Error(long mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
    }
}

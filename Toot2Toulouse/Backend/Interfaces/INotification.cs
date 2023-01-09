using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface INotification
    {
        void Warning(string mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);

        void Error(string mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
    }
}
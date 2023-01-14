using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface INotification
    {
        void Info(Guid userId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
        void Warning(Guid userId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);

        void Error(Guid userId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
    }
}
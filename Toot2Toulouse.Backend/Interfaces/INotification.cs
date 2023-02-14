using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface INotification
    {
        void Info(UserData user, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
        void Warning(UserData user, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);

        void Error(UserData user, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null);
    }
}
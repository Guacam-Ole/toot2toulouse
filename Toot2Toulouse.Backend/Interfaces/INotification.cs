using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.Messages;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface INotification
    {
        void Info(UserData user, MessageCodes messageCode, string? additionalInfo = null);
        void Warning(UserData user, MessageCodes messageCode, string? additionalInfo = null);

        void Error(UserData user, MessageCodes messageCode, string? additionalInfo = null);
    }
}
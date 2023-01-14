using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2Toulouse.Backend
{
    public class Notification : INotification
    {
        private readonly ILogger<Notification> _logger;
        private readonly IMastodon _mastodon;

        public Notification(ILogger<Notification> logger, IMastodon mastodon)
        {
            _logger = logger;
            _mastodon = mastodon;
        }

        public void Error(Guid id, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageTo(id, "💣 ERROR 💣\n", messageCode);
        }

        public void Info(Guid id, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageTo(id,null, messageCode);
        }

        public void Warning(Guid id, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageTo(id, "⚠️ WARNING ⚠️\n", messageCode);
        }
    }
}
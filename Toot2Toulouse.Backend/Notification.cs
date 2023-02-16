using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using static Toot2Toulouse.Backend.Configuration.Messages;

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

        public void Error(UserData user, MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageToAsync(user, "💣 ERROR 💣\n", messageCode, additionalInfo).Wait();
        }

        public void Info(UserData user, MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageToAsync(user, null, messageCode, additionalInfo).Wait();
        }

        public void Warning(UserData user, MessageCodes messageCode, string? additionalInfo = null)
        {
            _mastodon.SendStatusMessageToAsync(user, "⚠️ WARNING ⚠️\n", messageCode, additionalInfo).Wait();
        }
    }
}
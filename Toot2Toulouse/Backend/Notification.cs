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

        public void Error(string mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null)
        {
            _logger.LogInformation($"Error sent to {mastodonId}. Messageode:{messageCode}. AdditionalInfo:{additionalInfo}");
            // TODO: Send message to user
        }

        public void Warning(string mastodonId, TootConfigurationApp.MessageCodes messageCode, string? additionalInfo = null)
        {
            _logger.LogInformation($"Warning sent to {mastodonId}. Messagecode:{messageCode}. AdditionalInfo:{additionalInfo}");
            // TODO: Send message to user
        }
    }
}
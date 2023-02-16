using System.Text.Json.Serialization;

namespace Toot2Toulouse.Backend.Configuration
{
    public class Messages
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum MessageCodes
        {
            MastodonDown,
            TwitterDown,
            MastodonAuthError,
            TwitterAuthError,
            UpAndRunning,
            BackAgain,
            RegistrationFinished,
            Invite,
            RateLimit
        }
    }
}
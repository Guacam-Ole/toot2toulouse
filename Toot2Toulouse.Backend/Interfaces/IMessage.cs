using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IMessage
    {
        List<string>? GetThreadReplies(UserConfiguration userConfiguration, string originalToot, out string mainTweet);
    }
}

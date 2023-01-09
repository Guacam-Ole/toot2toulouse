using Toot2Toulouse.Backend.Configuration;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IToot
    {
        List<string>? GetReplies(string originalToot, out string mainTweet);
        void InitUser(UserConfiguration userConfiguration);
        string StripHtml(string content);
    }
}

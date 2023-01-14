using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IDatabase
    {
        UserData? GetUserById(Guid id);
        UserData? GetUserByIdAndHash(Guid id, string hash);
        void UpsertUser(UserData user, bool replaceExistingMastodonUser = false);
        void RemoveUser(Guid id);
        string CalculateHashForUser(UserData user);
    }
}

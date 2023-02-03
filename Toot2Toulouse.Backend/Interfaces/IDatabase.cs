using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IDatabase
    {
        UserData? GetUserById(Guid id);

        Guid? GetUserIdByMastodonId(string instance, string mastodonId);

        UserData? GetUserByIdAndHash(Guid id, string hash);

        void UpsertUser(UserData user);

        void RemoveUser(Guid id);

        string CalculateHashForUser(UserData user);

        List<UserData> GetActiveUsers();

        Stats GetServerStats();

        void UpSertServerStats(Stats stats);

        UserData GetUserByUsername(string handle, string instance);

        UserData GetUserByTwitterTmpGuid(string guid);

        List<UserData> GetAllUsers();


    }
}
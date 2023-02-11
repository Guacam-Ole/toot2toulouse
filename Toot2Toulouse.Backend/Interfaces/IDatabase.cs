using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IDatabase
    {
        Task<UserData?> GetUserById(Guid id);

        Task<Guid?> GetUserIdByMastodonId(string instance, string mastodonId);

        Task< UserData?> GetUserByIdAndHash(Guid id, string hash);

        Task UpsertUser(UserData user);

        Task RemoveUser(Guid id);

        string CalculateHashForUser(UserData user);

        Task<List<UserData>> GetActiveUsers();

        Task<Stats >GetServerStats();

        Task UpSertServerStats(Stats stats);

        Task<UserData> GetUserByUsername(string handle, string instance);

        Task<UserData> GetUserByTwitterTmpGuid(string guid);

        Task<List<UserData>> GetAllUsers();


    }
}
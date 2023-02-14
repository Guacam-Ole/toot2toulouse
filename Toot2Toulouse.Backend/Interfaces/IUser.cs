using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IUser
    {
        Task<UserData?> GetUser(Guid id, string hash);
        UserData ExportUserData(UserData userData);

        Task UpdateUser(UserData user);
        Task Block(Guid userId, UserData.BlockReasons reason);
        Task Unblock(Guid userId);
    }
}

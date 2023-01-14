using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IUser
    {
        UserData GetUserData();

        void DeleteUser();
        Task<bool> Login(Guid id, string hash);
    }
}

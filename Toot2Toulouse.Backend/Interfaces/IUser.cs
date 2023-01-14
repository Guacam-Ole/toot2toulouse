using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IUser
    {
        UserData? GetUser(Guid id, string hash);
        UserData? ExportUserData(UserData userData);
    }
}

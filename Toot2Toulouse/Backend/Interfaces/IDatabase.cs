using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend.Interfaces
{
    public interface IDatabase
    {
         User? GetUserById (Guid id);
         User? GetUserByIdAndHash (Guid id, string hash);
         void UpsertUser(User user);
        void RemoveUser(Guid id);
    }
}

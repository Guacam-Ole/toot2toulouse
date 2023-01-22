using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend
{
    public class User : IUser
    {
        private readonly ILogger<User> _logger;
        private readonly IDatabase _database;
        private readonly INotification _notification;

        public User(ILogger<User> logger,  IDatabase database, INotification notification)
        {
            _logger = logger;
            _database = database;
            _notification = notification;
        }

        public UserData? GetUser(Guid id, string hash)
        {
            return _database.GetUserByIdAndHash(id, hash);
        }

        public UserData? ExportUserData(UserData userData)
        {
            if (userData == null) return null;
            var expoortUserData = userData.Clone();
            expoortUserData.RemoveSecrets();
            _logger.LogDebug("Exported userdata for {user}", expoortUserData.Id);
            return expoortUserData;
        }

        public void UpdateUser(UserData user)
        {
            _database.UpsertUser(user);
        }
    }
}
using LiteDB;

using Microsoft.Extensions.Logging;

using System.Security.Cryptography;
using System.Text;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

namespace Toot2Toulouse.Backend
{
    public class Database : IDatabase
    {
        private readonly ILogger<Database> _logger;
        private readonly TootConfiguration _config;
        private string _path;

        public Database(ILogger<Database> logger, ConfigReader configReader, string path)
        {
            _logger = logger;
            //_webHostEnvironment = webHostEnvironment;
            _config = configReader.Configuration;
            _path = path;
        }

     

        private string GetDatabaseFile()
        {
            return Path.Combine(_path, "t2t.db");
            //return Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "t2t.db");
        }

        public UserData? GetUserById(Guid id)
        {
            try
            {
                using var db = new LiteDatabase(GetDatabaseFile());
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return userCollection.FindById(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving user {id}", id);
                return null;
            }
        }

        private string GetHashString(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));

            var sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }

        public string CalculateHashForUser(UserData user)
        {
            string valueToHash = $"{user.Id}{user.Mastodon.Id}{_config.Secrets.Salt}";
            return GetHashString(valueToHash);
        }

        public UserData? GetUserByIdAndHash(Guid id, string hash)
        {
            var user = GetUserById(id);
            if (user == null || CalculateHashForUser(user) != hash)
            {
                _logger.LogDebug("Retrieving user {id} failed. User missing or wrong hash", id);
                return null;
            }

            _logger.LogDebug("Retrieved user {user}", user);
            return user;
        }

        public void UpsertUser(UserData user, bool replaceExistingMastodonUser = false)
        {
            try
            {
                using var db = new LiteDatabase(GetDatabaseFile());
                var userCollection = db.GetCollection<UserData>(nameof(UserData));

                if (replaceExistingMastodonUser)
                {
                    var existingEntry = userCollection.Query().Where(q => q.Mastodon.Id == user.Mastodon.Id).FirstOrDefault();
                    if (existingEntry != null) user.Id = existingEntry.Id;
                }

                userCollection.Upsert(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed upserting user {user}", user);
                throw;
            }
        }

        public void RemoveUser(Guid id)
        {
            try
            {
                using var db = new LiteDatabase(GetDatabaseFile());
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                userCollection.Delete(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed removing user {id}", id);
                throw;
            }
        }
    }
}
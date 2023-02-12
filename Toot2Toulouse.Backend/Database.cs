using LiteDB;
using LiteDB.Async;

using Microsoft.Extensions.Logging;

using System.Net.Http.Headers;
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
        //private static ILiteDatabaseAsync _database;

        public Database(ILogger<Database> logger, ConfigReader configReader, string path)
        {
            _logger = logger;
            _config = configReader.Configuration;
            _path = path;
        }


        public async Task<UserData?> GetUserById(Guid id)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db")); 
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return await userCollection.FindByIdAsync(id);
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

        public async Task<UserData?> GetUserByIdAndHash(Guid id, string hash)
        {
            var user = await GetUserById(id);
            if (user == null || CalculateHashForUser(user) != hash)
            {
                _logger.LogDebug("Retrieving user {id} failed. User missing or wrong hash", id);
                return null;
            }

            _logger.LogDebug("Retrieved user {user}", user);
            return user;
        }

        public async Task<UserData> GetUserByUsername(string handle, string instance)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return (await userCollection.FindAsync(q => q.Mastodon.Handle == handle && q.Mastodon.Instance == instance)).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving user {handle}@{instance}", handle);
                return null;
            }
        }

        public async Task UpsertUser(UserData user)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                await userCollection.UpsertAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed upserting user {user}", user);
                throw;
            }
        }

        public async Task RemoveUser(Guid id)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                await userCollection.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed removing user {id}", id);
                throw;
            }
        }

        public async Task<List<UserData>> GetAllUsers()
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return (await userCollection.FindAsync(q => q.Id != Guid.Empty)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving users");
                throw;
            }
        }

        public async Task<List<UserData>> GetActiveUsers()
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return (await userCollection.FindAsync(q => q.Mastodon.Secret != null && q.Twitter.AccessSecret != null && q.BlockReason == null)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving users");
                throw;
            }
        }

        public async Task<UserData> GetUserByTwitterTmpGuid(string guid)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                return await userCollection.FindOneAsync(q => q.Twitter.TmpAuthGuid == guid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving users");
                throw;
            }
        }

        public async Task<Stats> GetServerStats()
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var statsCollection = db.GetCollection<Stats>(nameof(Stats));

                return (await statsCollection.FindAllAsync()).FirstOrDefault() ?? new Stats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed retrieving stats");
                throw;
            }
        }

        public async Task UpSertServerStats(Stats stats)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var statsCollection = db.GetCollection<Stats>(nameof(Stats));
                await statsCollection.DeleteAllAsync();
                await statsCollection.UpsertAsync(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating stats");
                throw;
            }
        }

        public async Task<Guid?> GetUserIdByMastodonId(string instance, string mastodonId)
        {
            try
            {
                using var db = new LiteDatabaseAsync(Path.Combine(_path, "t2t.db"));
                var userCollection = db.GetCollection<UserData>(nameof(UserData));
                var user = await userCollection.FindOneAsync(q => q.Mastodon.Id == mastodonId && q.Mastodon.Instance == instance);
                return user?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating stats");
                throw;
            }
        }
    }
}
using LiteDB;

using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;


namespace Toot2Toulouse.Backend
{
    public class Database : IDatabase
    {
        private readonly ILogger<Database> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public Database(ILogger<Database> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        private string GetDatabaseFile()
        {
            return Path.Combine(_webHostEnvironment.ContentRootPath, "Data", "t2t.db");
        }

        public User? GetUserById(Guid id)
        {
            using var db = new LiteDatabase(GetDatabaseFile());
            var userCollection = db.GetCollection<User>(nameof(User));
            return userCollection.FindById(id);
        }

        public User? GetUserByIdAndHash(Guid id, string hash)
        {
            var user=GetUserById(id);
            if (user == null || user.Hash!=hash) return null;
            return user;
        }

        public void UpsertUser(User user)
        {
            using var db = new LiteDatabase(GetDatabaseFile());
            var userCollection = db.GetCollection<User>(nameof(User));
            userCollection.Upsert(user);
        }

        public void RemoveUser(Guid id)
        {
            using var db = new LiteDatabase(GetDatabaseFile());
            var userCollection = db.GetCollection<User>(nameof(User));
            userCollection.Delete(id);
        }
    }
}

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Diagnostics.SymbolStore;
using System.IO.Pipes;

using Toot2Toulouse.Backend.Configuration;
using Toot2Toulouse.Backend.Interfaces;
using Toot2Toulouse.Backend.Models;

using Tweetinvi.Models;

namespace Toot2Toulouse.Backend
{
    public class User : Interfaces.IUser
    {
        private readonly ILogger<User> _logger;
        private readonly IDatabase _database;
        private readonly IMastodon _mastodon;
        private readonly IToulouse _toulouse;
        private readonly INotification _notification;
        private readonly TootConfiguration _config;
        private UserData? _authenticatedUser;

        public User(ILogger<User> logger, ConfigReader configReader, IDatabase database, IMastodon mastodon, IToulouse toulouse, INotification notification)
        {
            _logger = logger;
            _database = database;
            _mastodon = mastodon;
            _toulouse = toulouse;
            _notification = notification;
            _config = configReader.Configuration;
        }

        private void RetrieveUser(Guid id, string hash)
        {
            if (id == Guid.Empty)
            {
                _authenticatedUser = new UserData
                {
                    Id = Guid.Empty,
                    Mastodon = new Models.Mastodon { Handle = "dummy@mastodon.social", Id = "1", Instance = "mastodon.social", Secret = "VERY VERY SECRET" },
                    Twitter = new Models.Twitter { ConsumerSecret = "Also very secret", Id = "2", Handle = "@ocki" }
                };
                return;
            }
            _authenticatedUser = _database.GetUserByIdAndHash(id, hash);
        }

        public async Task<bool> Login(Guid id, string hash)
        {
            RetrieveUser(id, hash);
            if (_authenticatedUser == null) throw new Exception();
            
            await _toulouse.InitUserAsync(_authenticatedUser);
            //_toulouse.TweetServicePostsAsync().Wait();

            _notification.Info(id, TootConfigurationApp.MessageCodes.UpAndRunning);


            return _authenticatedUser != null;
        }

        public UserData? GetUserData()
        {
            if (_authenticatedUser == null) return null;
            var userExport = _authenticatedUser.Clone();
            userExport.RemoveSecrets();
            _logger.LogDebug("Exported userdata for {user}", _authenticatedUser);
            return userExport;
        }


        public void DeleteUser()
        {
            throw new NotImplementedException();
        }

        //public async Task CreateUser(string instance, string verificationCode)
        //{
        //    var mastodonUser = await _mastodon.GetUserAccountAsync(instance, verificationCode);
        //    // TODO: Check limits like isBot, allowedInstanced etc.

        //    if (mastodonUser == null)
        //    {
        //        _logger.LogDebug("Failed retrieving user on {instance}", instance);
        //        return;
        //    }

        //    _database.UpsertUser(new UserData
        //    {
        //        Config = _config.Defaults,
        //        Id = Guid.NewGuid(),
        //        Mastodon = new Models.Mastodon
        //        {
        //            Handle = mastodonUser.AccountName,
        //            Id = mastodonUser.Id,
        //            Instance = instance,
        //            Secret = verificationCode
        //        }
        //    }, true);
        //}
    }
}

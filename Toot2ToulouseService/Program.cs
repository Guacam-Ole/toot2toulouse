// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;
namespace Toot2ToulouseService
{

    public class Program
    {

        static async Task Main(string[] args)
        {
            var collection = new ServiceCollection();
            collection.AddScoped<ConfigReader>(cr => new ConfigReader("D:\\git\\private\\t2t\\Toot2Toulouse\\Properties")); // TODO: Config
            collection.AddScoped<ITwitter, Twitter>();
            collection.AddScoped<IMastodon, Mastodon>();
            collection.AddScoped<IToulouse, Toulouse>();
            collection.AddScoped<INotification, Notification>();
            collection.AddScoped<IMessage, Message>();
            collection.AddScoped<IDatabase, Database>(db => new Database(db.GetService<ILogger<Database>>(), db.GetService<ConfigReader>(), "D:\\git\\private\\t2t\\Toot2Toulouse\\Data"));    // TODO: Config
            collection.AddScoped<IUser, User>();

            collection.AddLogging(logging =>
            {
                // TODO
                //logging.ClearProviders();
                //logging.AddConsole();
                //logging.AddFile("data/app.log", append: true);
            });

            bool loop = args.Length > 0 && args[0] == "loop";

            // ...
            // Add other services
            // ...
            var serviceProvider = collection.BuildServiceProvider();
            var toulouse = serviceProvider.GetService<IToulouse>();


            do
            {
                await toulouse.SendTootsForAllUsers();
                if (loop) Thread.Sleep(1000 * 60); // TODO: Config
            } while (loop);

            serviceProvider.Dispose();
        }
    }
}
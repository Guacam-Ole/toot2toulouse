// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Text.Json.Nodes;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;

using Tweetinvi.Streams;

namespace Toot2ToulouseService
{


    public class Program
    {
        private static IToulouse? _toulouse;
        private static Publish? _publish;
        private static Maintenance? _maintenance;

        private static async Task Checkparameters(string[] args)
        {
            if (args.Length == 0)
            {
                await _publish.PublishToots(false);
            } else
            {
                switch (args[0])
                {
                    case "loop":
                        await _publish.PublishToots(true);
                        break;
                    case "upgrade":
                        if (args.Length>1)
                        {
                            _maintenance.Upgrade(new Version(args[1]));
                        } else
                        {
                            _maintenance.Upgrade(null);
                        }
                        break;
                    case "search":
                        if (args.Length<3)
                        {
                            Console.WriteLine("missing paramters: mastodonhandle, searchstring");
                            return;
                        }
                        await _publish.PublishTootsContaining(args[1], args[2], 100);
                        break;
                    default:
                        Console.WriteLine("Parameter unknown");
                        break;
                }
            }
        }

        static async Task Main(string[] args)
        {
            ReadBasicPaths(out string databasePath, out string configPath, out string logPath);
            var collection = new ServiceCollection();
            collection.AddScoped<ConfigReader>(cr => new ConfigReader(configPath));
            collection.AddScoped<ITwitter, Twitter>();
            collection.AddScoped<IMastodon, Mastodon>();
            collection.AddScoped<IToulouse, Toulouse>();
            collection.AddScoped<INotification, Notification>();
            collection.AddScoped<IMessage, Message>();
            collection.AddScoped<IDatabase, Database>(db => new Database(db.GetService<ILogger<Database>>(), db.GetService<ConfigReader>(), databasePath));
            collection.AddScoped<IUser, User>();
            collection.AddScoped<Publish>();
            collection.AddScoped<Maintenance>();

            collection.AddLogging(logging =>
            {
                //logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"))
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddFile(Path.Combine(logPath, "t2t.service.log"), append: true);
            });

            var serviceProvider = collection.BuildServiceProvider();
            _publish= serviceProvider.GetService<Publish>();
            _maintenance= serviceProvider.GetService<Maintenance>();

            await Checkparameters(args);

            serviceProvider.Dispose();
        }

        private static void ReadBasicPaths(out string databasePath, out string configPath, out string logPath)
        {
            //var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using var r = new StreamReader(Path.Combine(GetPropertiesPath(), "path.json"));
            string json = r.ReadToEnd();
            var pathConfig = JsonConvert.DeserializeObject<dynamic>(json);
            databasePath = pathConfig.database;
            configPath = pathConfig.config;
            logPath = pathConfig.log;
        }

        private static string GetPropertiesPath()
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(path, "Properties");
        }
    }
}
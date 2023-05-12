using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Text.Json;

using Toot2Toulouse.Backend;
using Toot2Toulouse.Backend.Interfaces;

namespace Toot2ToulouseService
{
    public class Startup
    {
        public void Inject(ServiceCollection services)
        {
            var config = ReadServiceConfig();

            services.AddScoped(cr => new ConfigReader(config.Paths.Config));
            services.AddScoped<ITwitter, Twitter>();
            services.AddScoped<IMastodon, Mastodon>();
            services.AddScoped<IToulouse, Toulouse>();
            services.AddScoped<INotification, Notification>();
            services.AddScoped<IMessage, Message>();
            services.AddScoped<IDatabase, Database>(db => new Database(db.GetService<ILogger<Database>>(), db.GetService<ConfigReader>(), config.Paths.Database));
            services.AddScoped<IUser, User>();
            services.AddScoped<Publish>();
            services.AddScoped<Maintenance>();

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(Enum.Parse<LogLevel>(config.LogLevel));
                var logFile = config.Paths.Log;
                if (!logFile.EndsWith(".log") && !logFile.EndsWith(".txt")) logFile = Path.Combine(logFile, "t2t.service.log");
                logging.AddFile(logFile, append: true);
            });
        }

        private static Config ReadServiceConfig()
        {
            using var r = new StreamReader(Path.Combine(GetPropertiesPath(), "config.json"));
            string json = r.ReadToEnd();
            return JsonSerializer.Deserialize<Config>(json, ConfigReader.JsonOptions);
        }

        private static string GetPropertiesPath()
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return path;
        }
    }
}
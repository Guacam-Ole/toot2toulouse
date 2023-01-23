using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Toot2Toulouse.Backend.Interfaces;

using Toot2Toulouse.Backend;

namespace Toot2ToulouseService
{
    public class Startup
    {
        public void Inject(ServiceCollection services)
        {
            ReadBasicPaths(out string databasePath, out string configPath, out string logPath);
            services.AddScoped(cr => new ConfigReader(configPath));
            services.AddScoped<ITwitter, Twitter>();
            services.AddScoped<IMastodon, Mastodon>();
            services.AddScoped<IToulouse, Toulouse>();
            services.AddScoped<INotification, Notification>();
            services.AddScoped<IMessage, Message>();
            services.AddScoped<IDatabase, Database>(db => new Database(db.GetService<ILogger<Database>>(), db.GetService<ConfigReader>(), databasePath));
            services.AddScoped<IUser, User>();
            services.AddScoped<Publish>();
            services.AddScoped<Maintenance>();

            services.AddLogging(logging =>
            {
                //logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"))
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddFile(Path.Combine(logPath, "t2t.service.log"), append: true);
            });
        }

        private static void ReadBasicPaths(out string databasePath, out string configPath, out string logPath)
        {
            //var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            using var r = new StreamReader(Path.Combine(GetPropertiesPath(), "path.json"));
            string json = r.ReadToEnd();
            var pathConfig = JsonSerializer.Deserialize<Paths>(json, ConfigReader.JsonOptions);
            databasePath = pathConfig.Database;
            configPath = pathConfig.Config;
            logPath = pathConfig.Log;
        }

        private static string GetPropertiesPath()
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(path, "Properties");
        }
    }
}

// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;

using Toot2Toulouse.Backend.Interfaces;

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
            }
            else
            {
                switch (args[0])
                {
                    case "loop":
                        await _publish.PublishToots(true);
                        break;

                    case "upgrade":
                        if (args.Length > 1)
                        {
                            _maintenance.Upgrade(new Version(args[1]));
                        }
                        else
                        {
                            _maintenance.Upgrade(null);
                        }
                        break;

                    case "version":
                        Console.WriteLine(_maintenance.GetVersion());
                        break;

                    case "search":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("missing paramters: mastodonhandle, searchstring");
                            return;
                        }
                        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(await _publish.GetTootsContaining(args[1], args[2], 5000), Toot2Toulouse.Backend.ConfigReader.JsonOptions));
                        break;

                    case "invite":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Recipient required");
                            return;
                        }
                        await _maintenance.Invite(args[1]);
                        break;

                    case "single":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("UserId and TootId required");
                            return;
                        }
                        await _publish.PublishSingleToot(new Guid(args[1]), args[2]);
                        break;

                    case "listids":
                        _maintenance.ListIds();
                        break;

                    default:
                        Console.WriteLine("Parameter unknown");
                        break;
                }
            }
        }

        private static async Task Main(string[] args)
        {
            var collection = new ServiceCollection();
            var startup = new Startup();
            startup.Inject(collection);

            var serviceProvider = collection.BuildServiceProvider();
            _publish = serviceProvider.GetService<Publish>();
            _maintenance = serviceProvider.GetService<Maintenance>();

            await Checkparameters(args);

            serviceProvider.Dispose();
        }
    }
}
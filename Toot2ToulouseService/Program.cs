﻿// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;

namespace Toot2ToulouseService
{
    public class Program
    {
        private static Publish _publish;
        private static Maintenance _maintenance;

        private static void ArgsCheck(string[] args, int requiredLength, string message)
        {
            if (args.Length < requiredLength)
            {
                throw new ArgumentException(message);
            }
        }

        private static async Task CheckparametersAsync(string[] args)
        {
            if (args.Length == 0)
            {
                await _publish.PublishTootsAsync(false);
            }
            else
            {
                switch (args[0])
                {
                    case "loop":
                        await _publish.PublishTootsAsync(true);
                        break;

                    case "upgrade":
                        if (args.Length > 1)
                        {
                            await _maintenance.Upgrade(new Version(args[1]));
                        }
                        else
                        {
                            await _maintenance.Upgrade(null);
                        }
                        break;

                    case "version":
                        Console.WriteLine(_maintenance.GetVersion());
                        break;

                    case "search":
                        ArgsCheck(args, 3, "missing paramters: mastodonhandle, searchstring");
                        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(await _publish.GetTootsContainingAsync(args[1], args[2], 5000), Toot2Toulouse.Backend.ConfigReader.JsonOptions));
                        break;

                    case "invite":
                        ArgsCheck(args, 2, "Recipient required");
                        await _maintenance.InviteAsync(args[1]);
                        break;

                    case "single":
                        ArgsCheck(args, 3, "UserId and TootId required");
                        await _publish.PublishSingleTootAsync(new Guid(args[1]), args[2]);
                        break;

                    case "listids":
                        await _maintenance.ListIds();
                        break;

                    case "block":
                        ArgsCheck(args, 3, "userId and reason [manual,authtwitter,authmastodon] required");
                        await _maintenance.BlockUser(new Guid(args[1]), Enum.Parse<Toot2Toulouse.Backend.Models.UserData.BlockReasons>(args[2], true));
                        break;

                    case "blockall":
                        ArgsCheck(args, 2, "reason [manual,authtwitter,authmastodon] required");
                        await _maintenance.BlockAllUsers(Enum.Parse<Toot2Toulouse.Backend.Models.UserData.BlockReasons>(args[1], true));
                        break;

                    case "messageall":
                        ArgsCheck(args, 2, "message required");
                        var messageArgs = args[1..].ToArray();
                        await _maintenance.MessageToAll(string.Join(' ', messageArgs));
                        break;

                    case "unblock":
                        ArgsCheck(args, 2, "userId required");
                        await _maintenance.UnblockUser(new Guid(args[1]));
                        break;

                    case "cleanup":
                        await _maintenance.CleanUp();
                        break;

                    case "stats":
                        await _maintenance.CollectStats();
                        break;

                    case "ping":
                        await _maintenance.Ping();
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
            _publish = serviceProvider.GetRequiredService<Publish>();
            _maintenance = serviceProvider.GetRequiredService<Maintenance>();

            await CheckparametersAsync(args);

            serviceProvider.Dispose();
        }
    }
}
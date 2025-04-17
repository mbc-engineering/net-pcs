using Mbc.Pcs.Net.Command;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace Cli
{
    /// <summary>
    /// The Main Program.
    /// </summary>
    public static class Program
    {
        private static ILogger _logger;

        public static async Task Main(string[] a)
        {
            SetupNLog();

            var client = new AdsClient();
            client.Connect("204.35.225.246.1.1", 851);

            var input = new Dictionary<string, object>
            {
                { "Val1", 1.7 },
                { "Val2", 2.5 },
            };

            var output = new Dictionary<string, object>
            {
                { "Result", null },
            };

            var count = 0;
            while (true)
            {
                var command = new PlcCommand(client, "Commands.fbAddCommand1", _logger);
                command.Timeout = TimeSpan.FromSeconds(60);
                command.StateChanged += (sender, args) => PrintProgress(args.Progress, args.SubTask);
                CancellationTokenSource cancellationToken = new CancellationTokenSource();
                var runningTask = Task.Factory.StartNew(() =>
                 {
                     bool quit = false;
                     do
                     {
                         quit = string.Equals(Console.ReadLine(), "q", StringComparison.OrdinalIgnoreCase);
                         if (quit)
                         {
                             cancellationToken.Cancel();
                         }
                     }
                     while (!quit);
                 });
                try
                {
                    Console.WriteLine(" press q + ENTER to cancel");
                    await command.ExecuteAsync(
                        cancellationToken.Token,
                        input: CommandInputBuilder.FromDictionary(input),
                        output: CommandOutputBuilder.FromDictionary(output));
                }
                catch
                {
                    Console.WriteLine(" canceled by user.");
                }

                Console.WriteLine(" => " + output["Result"]);
                Console.WriteLine(" press key to continue ");
                Console.ReadLine();
                count++;
            }
        }

        private static void PrintProgress(int progress, int subTask)
        {
            Console.CursorLeft = 0;
            var progressString = string.Join(string.Empty, Enumerable.Range(1, progress / 2).Select(x => "#"));
            Console.Write("{0}: {1}", subTask, progressString);
        }

        private static void SetupNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

            // Apply config
            NLog.LogManager.Configuration = config;

            _logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger("Cli");
        }
    }
}
